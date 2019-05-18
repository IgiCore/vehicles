using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using IgiCore.Vehicles.Client.Extensions;
using IgiCore.Vehicles.Shared;
using IgiCore.Vehicles.Shared.Models;
using JetBrains.Annotations;
using NFive.SDK.Client.Commands;
using NFive.SDK.Client.Events;
using NFive.SDK.Client.Extensions;
using NFive.SDK.Client.Input;
using NFive.SDK.Client.Interface;
using NFive.SDK.Client.Rpc;
using NFive.SDK.Client.Services;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Extensions;
using NFive.SDK.Core.Models;
using NFive.SDK.Core.Models.Player;
using NFive.SDK.Core.Rpc;
using Vehicle = IgiCore.Vehicles.Shared.Models.Vehicle;
using VehicleHash = CitizenFX.Core.VehicleHash;

namespace IgiCore.Vehicles.Client
{
	[PublicAPI]
	public class VehiclesService : Service
	{
		private Configuration config;
		private const int VehicleLoadDistance = 500;
		public List<TrackedVehicle> Tracked { get; set; } = new List<TrackedVehicle>();

		public VehiclesService(ILogger logger, ITickManager ticks, IEventManager events, IRpcHandler rpc,
			ICommandManager commands, OverlayManager overlay, User user) : base(logger, ticks, events, rpc, commands,
			overlay, user)
		{
		}

		public override async Task Started()
		{
			this.config = await this.Rpc.Event(VehicleEvents.GetConfiguration).Request<Configuration>();

			this.Rpc.Event(VehicleEvents.Spawn).On<Vehicle>(Spawn);
			this.Rpc.Event(VehicleEvents.Despawn).On<Vehicle>(Despawn);
			this.Rpc.Event(VehicleEvents.Transfer).On<Vehicle, int>(Transfer);
			this.Rpc.Event(VehicleEvents.Claim).On<Vehicle>(Claim);

			this.Ticks.Attach(OnTick);
			this.Ticks.Attach(DebugStuff);
		}

		public async Task DebugStuff()
		{
			if (Input.IsControlJustPressed(Control.InteractionMenu))
			{
				var carToSpawn = await this.Rpc.Event(VehicleEvents.CreateCar).Request<Vehicle>();

				carToSpawn.Hash = (uint) VehicleHash.Elegy;
				carToSpawn.Position = Game.PlayerPed.Position.ToPosition().InFrontOf(Game.PlayerPed.Heading, 10f);
				carToSpawn.PrimaryColor = new Shared.Models.VehicleColor
				{
					StockColor = VehicleStockColor.HotPink,
					CustomColor = new Color(),
					IsCustom = false
				};
				carToSpawn.SecondaryColor = new Shared.Models.VehicleColor
				{
					StockColor = VehicleStockColor.MattePurple,
					CustomColor = new Color(),
					IsCustom = false
				};
				carToSpawn.NeonColor = new Color();
				carToSpawn.PearlescentColor = VehicleStockColor.HotPink;
				carToSpawn.Seats = new List<Shared.Models.VehicleSeat>();
				carToSpawn.Wheels = new List<Shared.Models.VehicleWheel>();
				carToSpawn.Windows = new List<Shared.Models.VehicleWindow>();
				carToSpawn.Doors = new List<Shared.Models.VehicleDoor>();

				this.Spawn(null, carToSpawn.ToCar());
			}
		}

		public async Task OnTick()
		{
			Save();

			await this.Delay(20000);
		}

		private async void Spawn<T>(IRpcEvent e, T vehicle) where T : Vehicle
		{
			this.Logger.Debug("Spawn()");
			var spawnedVehicle = await vehicle.ToCitizenVehicle();
			API.VehToNet(spawnedVehicle.Handle);
			API.NetworkRegisterEntityAsNetworked(spawnedVehicle.Handle);
			var netId = API.NetworkGetNetworkIdFromEntity(spawnedVehicle.Handle);
			this.Logger.Debug($"Vehicle spawned | ID: {vehicle.Id} | NetId: {netId} | Handle: {spawnedVehicle.Handle}");
			var spawnedCar = spawnedVehicle.ToVehicle<Car>();
			spawnedCar.Id = vehicle.Id;
			spawnedCar.TrackingUserId = this.User.Id;
			spawnedCar.NetId = netId;

			this.Logger.Debug($"Spawn car save: {new Serializer().Serialize(spawnedCar)}");

			this.Rpc.Event(VehicleEvents.SaveCar).Trigger(spawnedCar);

			this.Tracked.Add(new TrackedVehicle
			{
				Id = spawnedCar.Id,
				Type = typeof(Car),
				NetId = spawnedCar.NetId ?? 0
			});
		}

		private void Despawn<T>(IRpcEvent e, T vehicle) where T : Vehicle
		{
			this.Logger.Debug("Despawn()");
			if (vehicle.NetId == null) return;
			var vehicleHandle = API.NetToVeh(vehicle.NetId ?? 0);

			var citVeh = new CitizenFX.Core.Vehicle(vehicleHandle);
			citVeh.Delete();

			this.Tracked.Remove(this.Tracked.FirstOrDefault(v => v.NetId == vehicle.NetId));

			this.Rpc.Event(VehicleEvents.Despawn).Trigger(vehicle.NetId);
		}

		private void Transfer(IRpcEvent e, Vehicle vehicle, int transferToHandle)
		{
			if (vehicle.NetId == null) return;

			this.Tracked.Remove(this.Tracked.FirstOrDefault(v => v.NetId == vehicle.NetId));

			this.Rpc.Event(VehicleEvents.Transfer).Trigger(vehicle, transferToHandle);
		}

		private void Claim(IRpcEvent e, Vehicle vehicle)
		{
			if (vehicle.NetId == null) return;

			var vehicleHandle = API.NetToVeh(vehicle.NetId ?? 0);
			var citVeh = new CitizenFX.Core.Vehicle(vehicleHandle);

			var spawnedVehicle = citVeh.ToVehicle<Vehicle>();

			spawnedVehicle.Id = vehicle.Id;
			spawnedVehicle.TrackingUserId = this.User.Id;
			spawnedVehicle.NetId = vehicle.NetId;

			this.Logger.Debug($"Claim vehicle save: {new Serializer().Serialize(spawnedVehicle)}");

			this.Rpc.Event(VehicleEvents.SaveCar).Trigger(spawnedVehicle);

			this.Tracked.Add(new TrackedVehicle
			{
				Id = spawnedVehicle.Id,
				Type = typeof(Car),
				NetId = spawnedVehicle.NetId ?? 0
			});
		}

		private void Save()
		{
			foreach (var trackedVehicle in this.Tracked)
			{
				var vehicleHandle = API.NetToVeh(trackedVehicle.NetId);

				var citVeh = new CitizenFX.Core.Vehicle(vehicleHandle);
				var netId = API.NetworkGetNetworkIdFromEntity(citVeh.Handle);

				switch (trackedVehicle.Type.VehicleType().Name)
				{
					case "Car":
						//var car = (Car)vehicle; // TODO: explicit converter
						//Add car specific props...
						var car = citVeh.ToVehicle<Car>();
						car.Id = trackedVehicle.Id;
						car.TrackingUserId = this.User.Id;
						car.NetId = netId;

						this.Rpc.Event(VehicleEvents.SaveCar).Trigger(car);
						break;

					default:
						var vehicle = citVeh.ToVehicle<Car>();
						vehicle.Id = trackedVehicle.Id;
						vehicle.TrackingUserId = this.User.Id;
						vehicle.NetId = netId;

						this.Rpc.Event($"igicore:vehicles:save:{trackedVehicle.Type.VehicleType().Name}")
							.Trigger(vehicle);
						break;
				}
			}
		}

		public class TrackedVehicle
		{
			public int Id { get; set; }
			public int NetId { get; set; }
			public Type Type { get; set; }
		}
	}
}
