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
using NFive.SDK.Core.Helpers;
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
		private const int VehicleLoadDistance = 500;
		public List<TrackedVehicle> Tracked { get; set; } = new List<TrackedVehicle>();

		public VehiclesService(ILogger logger, ITickManager ticks, IEventManager events, IRpcHandler rpc, ICommandManager commands, OverlayManager overlay, User user) : base(logger, ticks, events, rpc, commands, overlay, user)
		{
			this.Rpc.Event(VehicleEvents.Spawn).On<Vehicle>(Spawn);

			this.Ticks.Attach(OnTick);
			this.Ticks.Attach(DebugStuff);
		}

		public async Task DebugStuff()
		{
			if (Input.IsControlJustPressed(Control.InteractionMenu))
			{
				var carToSpawn = new Car
				{
					Hash = (uint)VehicleHash.Elegy,
					Position = Game.PlayerPed.Position.ToPosition().InFrontOf(Game.PlayerPed.Heading, 10f),
					PrimaryColor = new Shared.Models.VehicleColor
					{
						StockColor = VehicleStockColor.HotPink,
						CustomColor = new Color(),
						IsCustom = false
					},
					SecondaryColor = new Shared.Models.VehicleColor
					{
						StockColor = VehicleStockColor.MattePurple,
						CustomColor = new Color(),
						IsCustom = false
					},
					NeonColor = new Color(),
					PearlescentColor = VehicleStockColor.HotPink,
					Seats = new List<Shared.Models.VehicleSeat>(),
					Wheels = new List<Shared.Models.VehicleWheel>(),
					Windows = new List<Shared.Models.VehicleWindow>(),
					Doors = new List<Shared.Models.VehicleDoor>()
				};

				carToSpawn = await this.Rpc.Event(VehicleEvents.CreateCar).Request<Car>(carToSpawn);

				this.Spawn<Car>(null, carToSpawn);
			}
		}

		public async Task OnTick()
		{
			//Update();
			Save();

			await this.Delay(20000);
		}

		private async void Spawn<T>(IRpcEvent e, T vehicle) where T : Vehicle
		{
			var spawnedVehicle = await vehicle.ToCitizenVehicle();
			API.VehToNet(spawnedVehicle.Handle);
			API.NetworkRegisterEntityAsNetworked(spawnedVehicle.Handle);
			var netId = API.NetworkGetNetworkIdFromEntity(spawnedVehicle.Handle);
			this.Logger.Debug($"Vehicle spawned | ID: {vehicle.Id} | NetId: {netId} | Handle: {spawnedVehicle.Handle}");
			var spawnedCar = spawnedVehicle.ToVehicle<Car>(vehicle.Id);
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

		private void Update()
		{
			foreach (var trackedVehicle in this.Tracked.ToList())
			{
				var vehicleHandle = API.NetToVeh(trackedVehicle.NetId);
				var citVeh = new CitizenFX.Core.Vehicle(vehicleHandle);
				var closestPlayer = new Player(API.GetNearestPlayerToEntity(citVeh.Handle));

				if (closestPlayer == Game.Player || !API.NetworkIsPlayerConnected(closestPlayer.Handle))
				{
					if (!(Vector3.Distance(Game.Player.Character.Position, citVeh.Position) > VehicleLoadDistance)) continue;

					citVeh.Delete();
					this.Tracked.Remove(trackedVehicle);
					this.Rpc.Event(VehicleEvents.Unclaim).Trigger(trackedVehicle.NetId);
				}
				else
				{
					var netId = API.NetworkGetNetworkIdFromEntity(citVeh.Handle);

					var car = citVeh.ToVehicle<Car>(); // TODO: Make type ambiguous
					car.NetId = netId;

					this.Rpc.Event(VehicleEvents.Transfer).Trigger(car, closestPlayer.ServerId);
				}
			}
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
						var car = citVeh.ToVehicle<Car>(trackedVehicle.Id);
						car.TrackingUserId = this.User.Id;
						car.NetId = netId;

						this.Rpc.Event(VehicleEvents.SaveCar).Trigger(car);
						break;

					default:
						var vehicle = citVeh.ToVehicle<Car>(trackedVehicle.Id);

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
			public Guid Id { get; set; }
			public int NetId { get; set; }
			public Type Type { get; set; }
		}
	}
}
