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
			this.Ticks.Attach(OnTick);
			this.Ticks.Attach(DebugStuff);
		}

		public async Task DebugStuff()
		{
			if (Input.IsControlJustPressed(Control.InteractionMenu))
			{
				var car = new Car
				{
					Id = GuidGenerator.GenerateTimeBasedGuid(),
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
					PearlescentColor = VehicleStockColor.HotPink,
					Seats = new List<Shared.Models.VehicleSeat>(),
					Wheels = new List<Shared.Models.VehicleWheel>(),
					Windows = new List<Shared.Models.VehicleWindow>(),
					Doors = new List<Shared.Models.VehicleDoor>()
				};

				car = await this.Rpc.Event(VehicleEvents.CreateCar).Request<Car>(car);

				var spawnedVehicle = await car.ToCitizenVehicle();
				API.VehToNet(spawnedVehicle.Handle);
				API.NetworkRegisterEntityAsNetworked(spawnedVehicle.Handle);
				var netId = API.NetworkGetNetworkIdFromEntity(spawnedVehicle.Handle);

				var vehicle = await spawnedVehicle.ToVehicle(car.Id);
				vehicle.TrackingUserId = this.User.Id;
				vehicle.Handle = spawnedVehicle.Handle;
				vehicle.NetId = netId;

				this.Rpc.Event(VehicleEvents.SaveCar).Trigger(car);

				this.Tracked.Add(new TrackedVehicle
				{
					Id = car.Id,
					Type = typeof(Car),
					NetId = car.NetId ?? 0
				});
			}
		}

		public async Task OnTick()
		{
			await Update();
			await Save();

			await this.Delay(1000);
		}

		private async Task Update()
		{
			foreach (var trackedVehicle in this.Tracked.ToList())
			{
				var vehicleHandle = API.NetToVeh(trackedVehicle.NetId);
				var citVeh = new CitizenFX.Core.Vehicle(vehicleHandle);
				var closestPlayer = new CitizenFX.Core.Player(API.GetNearestPlayerToEntity(citVeh.Handle));

				if (closestPlayer == Game.Player || !API.NetworkIsPlayerConnected(closestPlayer.Handle))
				{
					if (!(Vector3.Distance(Game.Player.Character.Position, citVeh.Position) > VehicleLoadDistance)) continue;

					citVeh.Delete();
					this.Tracked.Remove(trackedVehicle);
					this.Rpc.Event($"igicore:vehicles:{trackedVehicle.Type.VehicleType().Name}:unclaim")
						.Trigger(trackedVehicle.NetId);
				}
				else
				{
					var netId = API.NetworkGetNetworkIdFromEntity(citVeh.Handle);

					var car = citVeh.ToCar(); // TODO: Make type ambiguous
					car.NetId = netId;

					this.Rpc.Event($"igicore:vehicles:{trackedVehicle.Type.VehicleType().Name}:transfer")
						.Trigger(car, closestPlayer.ServerId);
				}
			}
		}

		private async Task Save()
		{
			foreach (var trackedVehicle in this.Tracked)
			{
				var vehicleHandle = API.NetToVeh(trackedVehicle.NetId);
				var citVeh = new CitizenFX.Core.Vehicle(vehicleHandle);
				var netId = API.NetworkGetNetworkIdFromEntity(citVeh.Handle);

				var vehicle = await citVeh.ToVehicle(trackedVehicle.Id);

				vehicle.TrackingUserId = this.User.Id;
				vehicle.NetId = netId;
				vehicle.Hash = citVeh.Model.Hash;

				switch (trackedVehicle.Type.VehicleType().Name)
				{
					case "Car":
						//Car car = (Car)vehicle;
						// Add car specific props...
						this.Rpc.Event($"igicore:vehicles:{trackedVehicle.Type.VehicleType().Name}:save")
							.Trigger(vehicle);
						break;

					default:
						this.Rpc.Event($"igicore:vehicles:{trackedVehicle.Type.VehicleType().Name}:save")
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
