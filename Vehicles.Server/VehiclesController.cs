using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using IgiCore.Characters.Server;
using IgiCore.Characters.Server.Events;
using IgiCore.Vehicles.Server.Storage;
using IgiCore.Vehicles.Shared;
using IgiCore.Vehicles.Shared.Models;
using JetBrains.Annotations;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Models;
using NFive.SDK.Server;
using NFive.SDK.Server.Controllers;
using NFive.SDK.Server.Events;
using NFive.SDK.Server.Extensions;
using NFive.SDK.Server.Rcon;
using NFive.SDK.Server.Rpc;
using NFive.SDK.Server.Wrappers;
using Z.EntityFramework.Plus;
using Vector3 = CitizenFX.Core.Vector3;

namespace IgiCore.Vehicles.Server
{
	[PublicAPI]
	public class VehiclesController : ConfigurableController<Configuration>
	{
		private static readonly object SpawnLock = new object();
		private readonly CharacterManager characterManager;
		private readonly SessionManager sessionManager;
		private readonly IClientList clientList;

		[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
		public List<Vehicle> ActiveVehicles
		{
			get
			{
				using (var context = new StorageContext())
				{
					context.Configuration.ProxyCreationEnabled = false;

					return context.Vehicles.Where(v =>
						v.Deleted == null
						&& v.Position.X != float.MinValue
						&& v.Position.Y != float.MinValue
						&& v.Position.Z != float.MinValue
					).ToList();
				}
			}
		}

		public VehiclesController(ILogger logger, IEventManager events, IRpcHandler rpc, IRconManager rcon,
			Configuration configuration, IClientList clientList) : base(logger, events, rpc, rcon, configuration)
		{
			this.clientList = clientList;
			this.characterManager = new CharacterManager(this.Events, this.Rpc);
			this.sessionManager = new SessionManager(this.Events, this.Rpc);
		}

		public override Task Started()
		{
			this.Cleanup();

			// Attach event handlers.
			this.Rpc.Event(VehicleEvents.GetConfiguration).On(e => e.Reply(this.Configuration));
			this.Rpc.Event(VehicleEvents.CreateCar).On(Create);
			this.Rpc.Event(VehicleEvents.SaveCar).On<Car>(Save);
			this.Rpc.Event(VehicleEvents.Despawn).On<int>(Destroy);
			this.Rpc.Event(VehicleEvents.Transfer).On<int, Guid>(Transfer);
			this.characterManager.Selected += (r, e) => SpawnForPlayer(e);
			this.sessionManager.ClientDisconnected += (r, e) => OnClientDisconnect(e);

			// Start vehicle spawning thread.
			Task.Factory.StartNew(async () =>
			{
				while (true)
				{
					this.Spawn();
					await Task.Delay(this.Configuration.SpawnPollRate);
				}
			});


			// Start tracking update thread.
			Task.Factory.StartNew(async () =>
			{
				while (true)
				{
					Update();
					await Task.Delay(this.Configuration.TrackingPollRate);
				}
			});

			return base.Started();
		}

		protected void OnClientDisconnect(ClientSessionEventArgs e)
		{
			var characterSessions = this.characterManager.ActiveCharacterSessions;

			foreach (var vehicle in this.ActiveVehicles.Where(v => v.Handle != null).ToList())
			{
				var nearestCharacter = characterSessions
					.Where(c => c.IsConnected && c.SessionId != e.Session.Id)
					.Select(s => new
					{
						CharSession = s,
						Dist = Vector3.Distance(vehicle.Position.ToVector3().ToCitVector3() , s.Character.Position.ToVector3().ToCitVector3())
					})
					.OrderBy(c => c.Dist)
					.FirstOrDefault();
				if (nearestCharacter == null)
				{
					this.Destroy(null, vehicle.NetId ?? 0);
				}
				else
				{
					this.Transfer(null, vehicle.Id, nearestCharacter.CharSession.Session.UserId);
				}

			}
		}

		protected void Cleanup()
		{
			using (var context = new StorageContext())
			using (var transaction = context.Database.BeginTransaction())
			{
				context.Vehicles.Where(v => v.Hash == 0).Delete();

				var vehicles = context.Vehicles
					.Where(v => v.Handle != null || v.NetId != null || v.TrackingUserId != null).ToList();

				foreach (var vehicle in vehicles)
				{
					vehicle.Handle = null;
					vehicle.NetId = null;
					vehicle.TrackingUserId = Guid.Empty;
					context.Vehicles.AddOrUpdate(vehicle);
				}



				context.SaveChanges();
				transaction.Commit();
			}
		}

		protected async void Create(IRpcEvent e)
		{
			using (var context = new StorageContext())
			using (var transaction = context.Database.BeginTransaction())
			{
				var vehicle = new Vehicle
				{
					TrackingUserId = e.User.Id,
					Created = DateTime.UtcNow,
					Position = new Position
					{
						X = float.MinValue,
						Y = float.MinValue,
						Z = float.MinValue,
					},
					Rotation = new NFive.SDK.Core.Models.Vector3()
				};
				context.Vehicles.Add(vehicle);

				await context.SaveChangesAsync();
				transaction.Commit();

				e.Reply(vehicle);
			}
		}

		protected async void Save<T>(IRpcEvent e, T vehicle) where T : Vehicle
		{
			using (var context = new StorageContext())
			using (var transaction = context.Database.BeginTransaction())
			{
				var dbVeh = context.Vehicles
					.Include(v => v.Extras)
					.Include(v => v.Wheels)
					.Include(v => v.Doors)
					.Include(v => v.Windows)
					.Include(v => v.Seats)
					.Include(v => v.Mods)
					.FirstOrDefault(c => c.Id == vehicle.Id);

				if (dbVeh == null ||
				    dbVeh.TrackingUserId != Guid.Empty && e.User.Id != dbVeh.TrackingUserId) return;

				vehicle.Created = dbVeh.Created;

				context.Entry(dbVeh).CurrentValues.SetValues(vehicle);

				// Wheels
				foreach (var dbVehWheel in dbVeh.Wheels.ToList())
					if (vehicle.Wheels.All(m => m.Position != dbVehWheel.Position))
						context.VehicleWheels.Remove(dbVehWheel);

				foreach (var vehWheel in vehicle.Wheels)
				{
					var dbVehWheel = dbVeh.Wheels.SingleOrDefault(s => s.Position == vehWheel.Position);
					if (dbVehWheel != null)
					{
						vehWheel.Id = dbVehWheel.Id;
						vehWheel.VehicleId = vehicle.Id;
						context.Entry(dbVehWheel).CurrentValues.SetValues(vehWheel);
						// We have to manually set enums for some reason...
						context.Entry(dbVehWheel).Property("Position").CurrentValue = vehWheel.Position;
					}
					else dbVeh.Wheels.Add(vehWheel);
				}

				// Doors
				foreach (var dbVehDoor in dbVeh.Doors.ToList())
					if (vehicle.Doors.All(m => m.Index != dbVehDoor.Index))
						context.VehicleDoors.Remove(dbVehDoor);

				foreach (var vehDoor in vehicle.Doors)
				{
					var dbVehDoor = dbVeh.Doors.SingleOrDefault(s => s.Index == vehDoor.Index);
					if (dbVehDoor != null)
					{
						vehDoor.Id = dbVehDoor.Id;
						vehDoor.VehicleId = vehicle.Id;
						context.Entry(dbVehDoor).CurrentValues.SetValues(vehDoor);
					}
					else dbVeh.Doors.Add(vehDoor);
				}

				// Extras
				foreach (var dbVehExtra in dbVeh.Extras.ToList())
					if (vehicle.Extras.All(m => m.Index != dbVehExtra.Index))
						context.VehicleExtras.Remove(dbVehExtra);

				foreach (var vehExtra in vehicle.Extras)
				{
					var dbVehExtra = dbVeh.Extras.SingleOrDefault(s => s.Index == vehExtra.Index);
					if (dbVehExtra != null)
					{
						vehExtra.Id = dbVehExtra.Id;
						vehExtra.VehicleId = vehicle.Id;
						context.Entry(dbVehExtra).CurrentValues.SetValues(vehExtra);
					}
					else dbVeh.Extras.Add(vehExtra);
				}

				// Windows
				foreach (var dbVehWindow in dbVeh.Windows.ToList())
					if (vehicle.Windows.All(m => m.Index != dbVehWindow.Index))
						context.VehicleWindows.Remove(dbVehWindow);

				foreach (var vehWindow in vehicle.Windows)
				{
					var dbVehWindow = dbVeh.Windows.SingleOrDefault(s => s.Index == vehWindow.Index);
					if (dbVehWindow != null)
					{
						vehWindow.Id = dbVehWindow.Id;
						vehWindow.VehicleId = vehicle.Id;
						context.Entry(dbVehWindow).CurrentValues.SetValues(vehWindow);
					}
					else dbVeh.Windows.Add(vehWindow);
				}

				// Seats
				foreach (var dbVehSeat in dbVeh.Seats.ToList())
					if (vehicle.Seats.All(m => m.Index != dbVehSeat.Index))
						context.VehicleSeats.Remove(dbVehSeat);

				foreach (var vehSeat in vehicle.Seats)
				{
					var dbVehSeat = dbVeh.Seats.SingleOrDefault(s => s.Index == vehSeat.Index);
					if (dbVehSeat != null)
					{
						vehSeat.Id = dbVehSeat.Id;
						vehSeat.VehicleId = vehicle.Id;
						context.Entry(dbVehSeat).CurrentValues.SetValues(vehSeat);
					}
					else dbVeh.Seats.Add(vehSeat);
				}

				// TODO: Mods

				await context.SaveChangesAsync();
				transaction.Commit();
			}
		}

		protected async void Destroy(IRpcEvent e, int netId)
		{
			using (var context = new StorageContext())
			using (var transaction = context.Database.BeginTransaction())
			{
				var userId = e?.User.Id ?? Guid.Empty;
				var vehicle = context.Vehicles.FirstOrDefault(v => v.NetId == netId &&  (userId == Guid.Empty || v.TrackingUserId == userId));
				if (vehicle == null) return;

				vehicle.Handle = null;
				vehicle.NetId = null;
				vehicle.TrackingUserId = Guid.Empty;
				context.Vehicles.AddOrUpdate(vehicle);

				await context.SaveChangesAsync();
				transaction.Commit();
			}
		}

		protected void SpawnForPlayer(CharacterSessionEventArgs e)
		{
			lock (SpawnLock)
			{
				var vehiclesToSpawn = this.ActiveVehicles
					.Where(v =>
						v.Handle == null
						&& Vector3.Distance(v.Position.ToVector3().ToCitVector3(), e.CharacterSession.Character.Position.ToVector3().ToCitVector3()) <
						this.Configuration.DespawnDistance
					);

				foreach (var vehicle in vehiclesToSpawn)
				{
					// TODO: Await client response to prevent races.
					this.Rpc.Event(VehicleEvents.Spawn)
						.Target(this.clientList.Clients.First(c => c.Handle == e.CharacterSession.Session.Handle))
						.Trigger(vehicle);
				}
			}
		}

		protected void Spawn()
		{
			lock (SpawnLock)
			{
				var vehiclesToSpawn = this.ActiveVehicles.Where(v => v.Handle == null);
				var characterSessions = this.characterManager.ActiveCharacterSessions;

				foreach (var vehicle in vehiclesToSpawn)
				{
					var characterToSpawn = characterSessions
						.Where(c => c.IsConnected)
						.Select(s => new
						{
							Char = s.Character,
							Dist = Vector3.Distance(vehicle.Position.ToVector3().ToCitVector3(), s.Character.Position.ToVector3().ToCitVector3())
						})
						.Where(c => c.Dist < this.Configuration.DespawnDistance)
						.OrderBy(c => c.Dist)
						.FirstOrDefault();
					if (characterToSpawn == null) continue;
					var characterSession =
						characterSessions.FirstOrDefault(s => s.Character.Id == characterToSpawn.Char.Id);
					if (characterSession == null) continue;

					// TODO: Await client response to prevent races.
					this.Rpc.Event(VehicleEvents.Spawn)
						.Target(this.clientList.Clients.First(c => c.Handle == characterSession.Session.Handle))
						.Trigger(vehicle);
				}
			}
		}

		protected void Update()
		{
			var activeVehicles = this.ActiveVehicles.Where(v => v.Handle != null).ToList();

			var characterSessions = this.characterManager.ActiveCharacterSessions;

			foreach (var vehicle in activeVehicles)
			{
				var nearestCharacter = characterSessions
					.Where(c => c.IsConnected)
					.Select(s => new
					{
						CharSession = s,
						Dist = Vector3.Distance(vehicle.Position.ToVector3().ToCitVector3(), s.Character.Position.ToVector3().ToCitVector3())
					})
					.OrderBy(c => c.Dist)
					.FirstOrDefault();
				if (nearestCharacter == null) continue;
				if (nearestCharacter.Dist > this.Configuration.DespawnDistance)
				{
					Despawn(vehicle.NetId ?? 0, nearestCharacter.CharSession.Session.Handle);
				}
				else if (nearestCharacter.CharSession.Session.UserId != vehicle.TrackingUserId)
				{
					var trackingUser =
						characterSessions.FirstOrDefault(s => s.Session.UserId == vehicle.TrackingUserId);
					if (trackingUser == null) continue;

					this.Rpc.Event(VehicleEvents.Transfer)
						.Target(this.clientList.Clients.First(c => c.Handle == trackingUser.Session.Handle))
						.Trigger(vehicle.Id, nearestCharacter.CharSession.Session.UserId);
				}
			}
		}

		protected void Transfer(IRpcEvent e, int vehicleId, Guid transferToUserId)
		{
			var transferToHandle = this.characterManager.ActiveCharacterSessions.First(c => c.Session.UserId == transferToUserId).Session.Handle;
			var transferToClient = this.clientList.Clients.First(c => c.Handle == transferToHandle);
			var transferVehicle = this.ActiveVehicles.First(v => v.Id == vehicleId);

			this.Rpc.Event(VehicleEvents.Claim)
				.Target(transferToClient)
				.Trigger(vehicleId, transferVehicle.NetId);

			using (var context = new StorageContext())
			using (var transaction = context.Database.BeginTransaction())
			{
				context.Vehicles.First(v => v.Id == vehicleId).TrackingUserId = transferToUserId;
				context.SaveChanges();
				transaction.Commit();
			}
		}

		protected void Despawn(int vehicleNetId, int playerHandle)
		{
			this.Rpc.Event(VehicleEvents.Despawn).Target(this.clientList.Clients.First(c => c.Handle == playerHandle)).Trigger(vehicleNetId);
		}
	}
}
