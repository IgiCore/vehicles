using System;
using System.Collections.Generic;
using CitizenFX.Core;
using IgiCore.Vehicles.Shared.Models;
using NFive.SDK.Client.Extensions;
using Vector3 = NFive.SDK.Core.Models.Vector3;
using VehicleClass = IgiCore.Vehicles.Shared.Models.VehicleClass;
using VehicleColor = IgiCore.Vehicles.Shared.Models.VehicleColor;
using VehicleDoor = IgiCore.Vehicles.Shared.Models.VehicleDoor;
using VehicleDoorIndex = IgiCore.Vehicles.Shared.Models.VehicleDoorIndex;
using VehicleLockStatus = IgiCore.Vehicles.Shared.Models.VehicleLockStatus;
using VehicleSeat = IgiCore.Vehicles.Shared.Models.VehicleSeat;
using VehicleWheel = IgiCore.Vehicles.Shared.Models.VehicleWheel;
using VehicleWheelType = IgiCore.Vehicles.Shared.Models.VehicleWheelType;
using VehicleWindow = IgiCore.Vehicles.Shared.Models.VehicleWindow;
using VehicleWindowIndex = IgiCore.Vehicles.Shared.Models.VehicleWindowIndex;
using VehicleWindowTint = IgiCore.Vehicles.Shared.Models.VehicleWindowTint;

namespace IgiCore.Vehicles.Client.Extensions
{
	public static class CitizenVehicleExtensions
	{

		public static T ToVehicle<T>(this CitizenFX.Core.Vehicle vehicle) where T : IVehicle, new()
		{

			// Extras
			var vehicleExtras = new List<VehicleExtra>();
			for (var i = 0; i < 100; i++)
			{
				if (vehicle.ExtraExists(i)) vehicleExtras.Add(new VehicleExtra
					{
						Index = i,
						IsOn = vehicle.IsExtraOn(i),
					});
			}

			// Wheels
			var vehicleWheels = new List<VehicleWheel>();
			foreach (var wheelBoneName in VehicleWheelBones.Bones)
			{
				if (vehicle.Bones.HasBone(wheelBoneName.Value))
				{
					vehicleWheels.Add(new VehicleWheel
					{
						Type = (VehicleWheelType)vehicle.Mods.WheelType,
						Position = wheelBoneName.Key,
						Index = vehicle.Wheels[(int)wheelBoneName.Key].Index
					});
				}
			}

			// Doors
			var vehicleDoors = new List<VehicleDoor>();
			foreach (var vehicleDoor in vehicle.Doors)
			{
				vehicleDoors.Add(new VehicleDoor
				{
					Index = (VehicleDoorIndex)vehicleDoor.Index,
					IsBroken = vehicleDoor.IsBroken,
					IsOpen = vehicleDoor.IsOpen,
					Angle = vehicleDoor.AngleRatio
				});
			}

			// Windows
			var vehicleWindows = new List<VehicleWindow>();
			foreach (var value in Enum.GetValues(typeof(CitizenFX.Core.VehicleWindowIndex)))
			{
				var window = vehicle.Windows[(CitizenFX.Core.VehicleWindowIndex)value];
				vehicleWindows.Add(new VehicleWindow
				{
					Index = (VehicleWindowIndex)value,
					IsIntact = window.IsIntact,
					//TODO: Window rolled down state (has to be self-tracked)
				});
			}

			// Seats
			// TODO: Store player server IDs when communicating with the server and have the server assign a character
			var players = new PlayerList();
			var vehicleSeats = new List<VehicleSeat>();
			foreach (var vehicleOccupant in vehicle.Occupants)
			{
				foreach (var player in players)
				{
					if (player.Handle == vehicleOccupant.Handle)
					{
						vehicleSeats.Add(new VehicleSeat
						{
							Index = (VehicleSeatIndex)(int)vehicleOccupant.SeatIndex,
						});
					}
				}
			}

			var neonPositions = VehicleNeonPositions.None;
			if (vehicle.Mods.IsNeonLightsOn(VehicleNeonLight.Back)) neonPositions |= VehicleNeonPositions.Back;
			if (vehicle.Mods.IsNeonLightsOn(VehicleNeonLight.Front)) neonPositions |= VehicleNeonPositions.Front;
			if (vehicle.Mods.IsNeonLightsOn(VehicleNeonLight.Right)) neonPositions |= VehicleNeonPositions.Right;
			if (vehicle.Mods.IsNeonLightsOn(VehicleNeonLight.Left)) neonPositions |= VehicleNeonPositions.Left;

			return new T
			{
				Hash = vehicle.Model.Hash,
				Handle = vehicle.Handle,
				Position = vehicle.Position.ToPosition(),
				Heading = vehicle.Heading,
				Rotation = new Vector3(vehicle.Rotation.X, vehicle.Rotation.Y, vehicle.Rotation.Z),
				SteeringAngle = vehicle.SteeringAngle,
				BodyHealth = vehicle.BodyHealth,
				EngineHealth = vehicle.EngineHealth,
				DirtLevel = vehicle.DirtLevel,
				FuelLevel = vehicle.FuelLevel,
				OilLevel = vehicle.OilLevel,
				PetrolTankHealth = vehicle.PetrolTankHealth,
				PrimaryColor = new VehicleColor
				{
					StockColor = (VehicleStockColor)vehicle.Mods.PrimaryColor,
					CustomColor = vehicle.Mods.CustomPrimaryColor.ToColor(),
					IsCustom = vehicle.Mods.IsPrimaryColorCustom
				},
				SecondaryColor = new VehicleColor
				{
					StockColor = (VehicleStockColor)vehicle.Mods.SecondaryColor,
					CustomColor = vehicle.Mods.CustomSecondaryColor.ToColor(),
					IsCustom = vehicle.Mods.IsSecondaryColorCustom
				},
				PearlescentColor = (VehicleStockColor)vehicle.Mods.PearlescentColor,
				RimColor = (VehicleStockColor)vehicle.Mods.RimColor,
				TrimColor = (VehicleStockColor)vehicle.Mods.TrimColor,
				DashboardColor = (VehicleStockColor)vehicle.Mods.DashboardColor,
				NeonColor = vehicle.Mods.NeonLightsColor.ToColor(),
				NeonPositions = neonPositions,
				TireSmokeColor = vehicle.Mods.TireSmokeColor.ToColor(),
				WindowTint = (VehicleWindowTint)vehicle.Mods.WindowTint,
				Class = (VehicleClass)vehicle.ClassType,
				LockStatus = (VehicleLockStatus)vehicle.LockStatus,
				CanTiresBurst = vehicle.CanTiresBurst,
				NeedsToBeHotWired = vehicle.NeedsToBeHotwired,
				IsVehicleConvertible = vehicle.IsConvertible,
				HasRoof = vehicle.HasRoof,
				IsRoofOpen = vehicle.RoofState != VehicleRoofState.Closed,
				IsRightHeadLightBroken = vehicle.IsRightHeadLightBroken,
				IsLeftHeadLightBroken = vehicle.IsLeftHeadLightBroken,
				IsRearBumperBrokenOff = vehicle.IsRearBumperBrokenOff,
				IsFrontBumperBrokenOff = vehicle.IsFrontBumperBrokenOff,
				IsTaxiLightOn = vehicle.IsTaxiLightOn,
				IsSearchLightOn = vehicle.IsSearchLightOn,
				//IsInteriorLightOn = vehicle.IsInteriorLightOn,  // < THIS WILL CRASH THE GAME BECAUSE FUCK YOU THAT'S WHY!
				IsLightsOn = vehicle.AreLightsOn,
				IsHighBeamsOn = vehicle.AreHighBeamsOn,
				IsEngineRunning = vehicle.IsEngineRunning,
				IsDrivable = vehicle.IsDriveable,
				IsAlarmed = vehicle.IsAlarmSet,
				IsAlarmSounding = vehicle.IsAlarmSounding,
				LicensePlate = vehicle.Mods.LicensePlate,
				Extras = vehicleExtras,
				Wheels = vehicleWheels,
				Doors = vehicleDoors,
				Seats = vehicleSeats,
				Windows = vehicleWindows
			};
		}
	}
}
