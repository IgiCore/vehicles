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
			this.Rpc.Event(VehicleEvents.Despawn).On<int>(Despawn);
			this.Rpc.Event(VehicleEvents.Transfer).On<int, Guid>(Transfer);
			this.Rpc.Event(VehicleEvents.Claim).On<int, int>(Claim);

			this.Ticks.Attach(OnTick);
			this.Ticks.Attach(DebugStuff);
		}

		public async Task DebugStuff()
		{
			if (Input.IsControlJustPressed(Control.InteractionMenu))
			{
				var carToSpawn = await this.Rpc.Event(VehicleEvents.CreateCar).Request<Vehicle>();

				carToSpawn.Hash = (uint) VehicleHash.Elegy;
				carToSpawn.Position = Game.PlayerPed.Position.ToVector3().ToPosition().InFrontOf(Game.PlayerPed.Heading, 10f);
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
			SaveTracked();

			await this.Delay(this.config.AutosaveRate);
		}

		private async void Spawn<T>(IRpcEvent e, T vehicle) where T : Vehicle
		{
			try
			{
				var spawnedVehicle = await vehicle.ToCitizenVehicle();
				API.VehToNet(spawnedVehicle.Handle);
				API.NetworkRegisterEntityAsNetworked(spawnedVehicle.Handle);
				var netId = API.NetworkGetNetworkIdFromEntity(spawnedVehicle.Handle);
				var spawnedCar = spawnedVehicle.ToVehicle<Car>();
				spawnedCar.Id = vehicle.Id;
				spawnedCar.TrackingUserId = this.User.Id;
				spawnedCar.NetId = netId;

				this.Rpc.Event(VehicleEvents.SaveCar).Trigger(spawnedCar);

				this.Tracked.Add(new TrackedVehicle
				{
					Id = spawnedCar.Id,
					Type = typeof(Car),
					NetId = spawnedCar.NetId ?? 0
				});
			}
			catch (Exception exception)
			{
				this.Logger.Error(exception, $"Failed to spawn vehicle with ID {vehicle.Id}");
			}

		}

		private void Despawn(IRpcEvent e, int vehicleNetId)
		{
			if (API.NetworkDoesNetworkIdExist(vehicleNetId))
			{
				var citVeh = new CitizenFX.Core.Vehicle(API.NetToVeh(vehicleNetId));
				citVeh.Delete();
			}

			this.Tracked.Remove(this.Tracked.FirstOrDefault(v => v.NetId == vehicleNetId));
			this.Rpc.Event(VehicleEvents.Despawn).Trigger(vehicleNetId);
		}

		private void Transfer(IRpcEvent e, int vehicleId, Guid transferToUserId)
		{
			this.Tracked.Remove(this.Tracked.First(v => v.Id == vehicleId));
			this.Rpc.Event(VehicleEvents.Transfer).Trigger(vehicleId, transferToUserId);
		}

		private void Claim(IRpcEvent e, int vehicleId, int vehicleNetId)
		{
			this.Tracked.Add(new TrackedVehicle
			{
				Id = vehicleId,
				Type = typeof(Car),
				NetId = vehicleNetId
			});
		}

		private void SaveTracked()
		{
			foreach (var trackedVehicle in this.Tracked)
			{
				if (!API.NetworkDoesNetworkIdExist(trackedVehicle.NetId))
				{
					this.Despawn(null, trackedVehicle.NetId);
					continue;
				}
				var vehicleHandle = API.NetToVeh(trackedVehicle.NetId);

				if (!API.DoesEntityExist(vehicleHandle))
				{
					this.Despawn(null, trackedVehicle.NetId);
					continue;
				}
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
