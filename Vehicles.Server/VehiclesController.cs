using System;
using System.Data.Entity;
using System.Linq;
using CitizenFX.Core;
using IgiCore.Characters.Shared;
using IgiCore.Vehicles.Server.Storage;
using IgiCore.Vehicles.Shared;
using IgiCore.Vehicles.Shared.Models;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Helpers;
using NFive.SDK.Core.Models;
using NFive.SDK.Core.Rpc;
using NFive.SDK.Server.Controllers;
using NFive.SDK.Server.Events;
using NFive.SDK.Server.Extensions;
using NFive.SDK.Server.Rcon;
using NFive.SDK.Server.Rpc;
using Z.EntityFramework.Plus;

namespace IgiCore.Vehicles.Server
{
	[PublicAPI]
	public class VehiclesController : ConfigurableController<Configuration>
	{
		public VehiclesController(ILogger logger, IEventManager events, IRpcHandler rpc, IRconManager rcon, Configuration configuration) : base(logger, events, rpc, rcon, configuration)
		{
			this.Rpc.Event(VehicleEvents.CreateCar).On<Car>(Create);
			this.Rpc.Event(VehicleEvents.SaveCar).On<Car>(Save);
			this.Rpc.Event(CharacterEvents.SavePosition).On<Guid, Position>(LoadNearby);

			this.Cleanup();
		}

		protected void Cleanup()
		{
			using (var context = new StorageContext())
			{
				context.Vehicles.Where(v => v.Handle != null || v.NetId != null || v.TrackingUserId != null).Update(v => new Vehicle
				{
					Handle = null,
					NetId = null,
					TrackingUserId = null,
				});
				context.SaveChanges();
			}
		}

		protected async void Create<T>(IRpcEvent e, T vehicle) where T : class, IVehicle
		{
			using (var context = new StorageContext())
			{
				vehicle.Id = GuidGenerator.GenerateTimeBasedGuid();
				vehicle.Created = DateTime.UtcNow;
				vehicle.TrackingUserId = e.User.Id;
				context.Set<T>().Add(vehicle);
				await context.SaveChangesAsync();
			}

			e.Reply(vehicle);
		}


		protected async void Save<T>(IRpcEvent e, T vehicle) where T : Vehicle
		{
			this.Logger.Debug($"Saving vehicle: {new Serializer().Serialize(vehicle)}");

			using (var context = new StorageContext())
			{
				if (vehicle.Id == Guid.Empty) vehicle.Id = context.Set<T>().FirstOrDefault(c => c.Handle == vehicle.Handle)?.Id ?? Guid.Empty;
				if (vehicle.Id == Guid.Empty) return;

				var dbVeh = context.Vehicles
					.Include(v => v.Extras) // TODO: Including these throws errors when there are values in the tables.
					.Include(v => v.Wheels)
					.Include(v => v.Doors)
					.Include(v => v.Windows)
					.Include(v => v.Seats)
					.Include(v => v.Mods)
					.FirstOrDefault(c => c.Id == vehicle.Id);

				if (dbVeh == null || dbVeh.TrackingUserId != null && vehicle.TrackingUserId != dbVeh.TrackingUserId) return;

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
						context.Entry(dbVehSeat).CurrentValues.SetValues(vehSeat);
					}
					else dbVeh.Seats.Add(vehSeat);
				}

				// TODO: Mods

				await context.SaveChangesAsync();

			}
		}

		protected void LoadNearby(IRpcEvent e, Guid playerGuid, Position position)
		{
			using (var context = new StorageContext())
			{
				this.Logger.Debug($"DespawnDistance: {this.Configuration.DespawnDistance}");
				this.Logger.Debug($"Checking for vehicles near position: {position}");

				foreach (var vehicle in context.Vehicles
					.Where(v =>
						v.Handle == null
						&& v.Position.X > position.X - this.Configuration.DespawnDistance
						&& v.Position.X < position.X + this.Configuration.DespawnDistance
						&& v.Position.Y > position.Y - this.Configuration.DespawnDistance
						&& v.Position.Y < position.Y + this.Configuration.DespawnDistance
					)
					.ToArray()
					.Where(v => Vector3.Distance(v.Position.ToVector3(), position.ToVector3()) < this.Configuration.DespawnDistance)
				)
				{
					this.Logger.Debug($"Spawning vehicle: {vehicle.Id} | Player: {playerGuid}");
					e.Client.Event(VehicleEvents.Spawn).Trigger(vehicle);
				}
			}
		}
	}
}
