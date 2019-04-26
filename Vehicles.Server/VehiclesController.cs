using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using IgiCore.Characters.Server;
using IgiCore.Characters.Server.Events;
using IgiCore.Vehicles.Server.Storage;
using IgiCore.Vehicles.Shared;
using IgiCore.Vehicles.Shared.Models;
using JetBrains.Annotations;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Models;
using NFive.SDK.Server.Controllers;
using NFive.SDK.Server.Events;
using NFive.SDK.Server.Extensions;
using NFive.SDK.Server.Rcon;
using NFive.SDK.Server.Rpc;
using NFive.SDK.Server.Wrappers;
using Client = IgiCore.Vehicles.Server.Rpc.Client;

namespace IgiCore.Vehicles.Server
{
	[PublicAPI]
	public class VehiclesController : ConfigurableController<Configuration>
	{
		private static readonly object SpawnLock = new object();
		private readonly CharacterManager characterManager;
		private readonly SessionManager sessionManager;

		public VehiclesController(ILogger logger, IEventManager events, IRpcHandler rpc, IRconManager rcon,
			Configuration configuration) : base(logger, events, rpc, rcon, configuration)
		{
			this.characterManager = new CharacterManager(this.Events, this.Rpc);
			this.sessionManager = new SessionManager(this.Events, this.Rpc);

			this.Rpc.Event(VehicleEvents.CreateCar).On(Create);
			this.Rpc.Event(VehicleEvents.SaveCar).On<Car>(Save);
			this.Rpc.Event(VehicleEvents.Destroy).On<int>(Destroy);

			this.characterManager.Selected += (r, e) => SpawnForPlayer(e);

			this.sessionManager.ClientDisconnected += (r, e) => OnClientDisconnect(e);

			this.Cleanup();

			Task.Factory.StartNew(async () =>
			{
				while (true)
				{
					lock (SpawnLock)
					{
						this.Spawn();
					}

					await Task.Delay(this.Configuration.SpawnPollRate);
				}
				// ReSharper disable once FunctionNeverReturns
			});

			Task.Factory.StartNew(async () =>
			{
				while (true)
				{
					await Task.Delay(this.Configuration.TrackingPollRate);
				}
				// ReSharper disable once FunctionNeverReturns
			});
		}

		protected void OnClientDisconnect(ClientSessionEventArgs e)
		{
		}

		protected void Cleanup()
		{
			using (var context = new StorageContext())
			{
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
			}
		}

		protected async void Create(IRpcEvent e)
		{
			using (var context = new StorageContext())
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
				};
				context.Vehicles.Add(vehicle);
				await context.SaveChangesAsync();

				e.Reply(vehicle);
			}
		}

		protected async void Save<T>(IRpcEvent e, T vehicle) where T : Vehicle
		{
			using (var context = new StorageContext())
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
				    dbVeh.TrackingUserId != Guid.Empty && vehicle.TrackingUserId != dbVeh.TrackingUserId) return;

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
			}
		}

		protected async void Destroy(IRpcEvent e, int netId)
		{
			using (var context = new StorageContext())
			using (var transaction = context.Database.BeginTransaction())
			{
				var vehicle = await context.Vehicles
					.Where(v => v.NetId == netId && v.TrackingUserId == e.User.Id)
					.FirstAsync();

				vehicle.Handle = null;
				vehicle.NetId = null;
				vehicle.TrackingUserId = Guid.Empty;
				context.Vehicles.AddOrUpdate(vehicle);

				await context.SaveChangesAsync();
				transaction.Commit();
			}
		}

		[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
		protected void SpawnForPlayer(CharacterSessionEventArgs e)
		{
			using (var context = new StorageContext())
			{
				var vehiclesToSpawn = context.Vehicles.Where(v =>
					v.Handle == null
					&& v.Position.X != float.MinValue
					&& v.Position.Y != float.MinValue
					&& v.Position.Z != float.MinValue
				).Where(v =>
					Vector3.Distance(v.Position.ToVector3(), e.CharacterSession.Character.Position.ToVector3()) <
					this.Configuration.DespawnDistance);

				foreach (var vehicle in vehiclesToSpawn)
				{
					this.Rpc.Event(VehicleEvents.Spawn).Trigger(new Client(e.CharacterSession.Session.Handle), vehicle);
				}
			}
		}

		[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
		protected void Spawn()
		{
			using (var context = new StorageContext())
			{
				var vehiclesToSpawn = context.Vehicles.Where(v =>
					v.Handle == null
					&& v.Position.X != float.MinValue
					&& v.Position.Y != float.MinValue
					&& v.Position.Z != float.MinValue
				).ToArray();
				var characterSessions = this.characterManager.ActiveCharacterSessions;

				foreach (var vehicle in vehiclesToSpawn)
				{
					var characterToSpawn = characterSessions
						.Select(s => new
						{
							Char = s.Character,
							Dist = Vector3.Distance(vehicle.Position.ToVector3(), s.Character.Position.ToVector3())
						})
						.Where(c => c.Dist < this.Configuration.DespawnDistance)
						.OrderBy(c => c.Dist)
						.FirstOrDefault();
					if (characterToSpawn == null) continue;
					var characterSession =
						characterSessions.FirstOrDefault(s => s.Character.Id == characterToSpawn.Char.Id);
					if (characterSession == null) continue;

					this.Rpc.Event(VehicleEvents.Spawn).Trigger(new Client(characterSession.Session.Handle), vehicle);
				}
			}
		}

		protected void Despawn(Guid vehicleId)
		{
			using (var context = new StorageContext())
			{
				var vehiclesToDespawn = context.Vehicles.Where(v =>
					v.Handle != null
				).ToArray();

				foreach (var vehicle in vehiclesToDespawn)
				{
					this.Rpc.Event(VehicleEvents.Destroy).Trigger();
				}
			}
		}
	}
}
