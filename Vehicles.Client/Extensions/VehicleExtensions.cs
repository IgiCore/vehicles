using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using IgiCore.Vehicles.Shared.Models;
using NFive.SDK.Client.Extensions;
using Vehicle = IgiCore.Vehicles.Shared.Models.Vehicle;

namespace IgiCore.Vehicles.Client.Extensions
{
	public static class VehicleExtensions
	{
		public static Type VehicleType(this Type type)
		{
			var baseType = type.BaseType;

			return baseType != null && baseType.IsSubclassOf(typeof(Vehicle)) ? baseType : type;
		}

		public static Type VehicleType(this IVehicle vehicle)
		{
			var baseType = vehicle.GetType().BaseType;

			return baseType != null && baseType.IsSubclassOf(typeof(Vehicle)) ? baseType : vehicle.GetType();
		}

		public static async Task<CitizenFX.Core.Vehicle> ToCitizenVehicle(this Vehicle vehicle)
		{
			var citizenVehicle = await World.CreateVehicle(new Model((int)vehicle.Hash), vehicle.Position.ToVector3(), vehicle.Heading);

			citizenVehicle.BodyHealth = vehicle.BodyHealth;
			citizenVehicle.EngineHealth = vehicle.EngineHealth;
			citizenVehicle.DirtLevel = vehicle.DirtLevel;
			citizenVehicle.FuelLevel = vehicle.FuelLevel;
			citizenVehicle.OilLevel = vehicle.OilLevel;
			citizenVehicle.PetrolTankHealth = vehicle.PetrolTankHealth;
			citizenVehicle.TowingCraneRaisedAmount = vehicle.TowingCraneRaisedAmount;
			//citizenVehicle.HasAlarm = vehicle.HasAlarm;
			citizenVehicle.IsAlarmSet = vehicle.IsAlarmed;
			//citizenVehicle.HasLock = vehicle.HasLock;
			citizenVehicle.IsDriveable = vehicle.IsDrivable;
			citizenVehicle.IsEngineRunning = vehicle.IsEngineRunning;
			//citizenVehicle.HasSeatbelts = vehicle.HasSeatBelts;
			citizenVehicle.AreHighBeamsOn = vehicle.IsHighBeamsOn;
			citizenVehicle.AreLightsOn = vehicle.IsLightsOn;
			citizenVehicle.IsInteriorLightOn = vehicle.IsInteriorLightOn;
			citizenVehicle.IsSearchLightOn = vehicle.IsSearchLightOn;
			citizenVehicle.IsTaxiLightOn = vehicle.IsTaxiLightOn;
			citizenVehicle.IsLeftIndicatorLightOn = vehicle.IsLeftIndicatorLightOn;
			citizenVehicle.IsRightIndicatorLightOn = vehicle.IsRightIndicatorLightOn;
			//citizenVehicle.IsFrontBumperBrokenOff = vehicle.IsFrontBumperBrokenOff;
			//citizenVehicle.IsRearBumperBrokenOff = vehicle.IsRearBumperBrokenOff;
			citizenVehicle.IsLeftHeadLightBroken = vehicle.IsLeftHeadLightBroken;
			citizenVehicle.IsRightHeadLightBroken = vehicle.IsRightHeadLightBroken;
			citizenVehicle.IsRadioEnabled = vehicle.IsRadioEnabled;
			citizenVehicle.RoofState = vehicle.IsRoofOpen ? VehicleRoofState.Opened : VehicleRoofState.Closed;
			citizenVehicle.NeedsToBeHotwired = vehicle.NeedsToBeHotWired;
			citizenVehicle.CanTiresBurst = vehicle.CanTiresBurst;

			if (vehicle.PrimaryColor.IsCustom) citizenVehicle.Mods.CustomPrimaryColor = vehicle.PrimaryColor.CustomColor.ToCitColor();
			else citizenVehicle.Mods.PrimaryColor = (CitizenFX.Core.VehicleColor) vehicle.PrimaryColor.StockColor;

			if (vehicle.SecondaryColor.IsCustom) citizenVehicle.Mods.CustomSecondaryColor = vehicle.SecondaryColor.CustomColor.ToCitColor();
			else citizenVehicle.Mods.SecondaryColor = (CitizenFX.Core.VehicleColor) vehicle.SecondaryColor.StockColor;

			citizenVehicle.Mods.PearlescentColor = (CitizenFX.Core.VehicleColor) vehicle.PearlescentColor;
			citizenVehicle.Mods.RimColor = (CitizenFX.Core.VehicleColor) vehicle.RimColor;
			citizenVehicle.Mods.TrimColor = (CitizenFX.Core.VehicleColor) vehicle.TrimColor;
			citizenVehicle.Mods.DashboardColor = (CitizenFX.Core.VehicleColor) vehicle.DashboardColor;
			citizenVehicle.Mods.NeonLightsColor = vehicle.NeonColor.ToCitColor();

			citizenVehicle.Mods.SetNeonLightsOn(VehicleNeonLight.Back, vehicle.NeonPositions.HasFlag(VehicleNeonPositions.Back));
			citizenVehicle.Mods.SetNeonLightsOn(VehicleNeonLight.Front, vehicle.NeonPositions.HasFlag(VehicleNeonPositions.Front));
			citizenVehicle.Mods.SetNeonLightsOn(VehicleNeonLight.Left, vehicle.NeonPositions.HasFlag(VehicleNeonPositions.Left));
			citizenVehicle.Mods.SetNeonLightsOn(VehicleNeonLight.Right, vehicle.NeonPositions.HasFlag(VehicleNeonPositions.Right));

			citizenVehicle.Mods.WindowTint = (CitizenFX.Core.VehicleWindowTint) vehicle.WindowTint;
			citizenVehicle.LockStatus = (CitizenFX.Core.VehicleLockStatus) vehicle.LockStatus;

			//citizenVehicle.RadioStation = (RadioStation)(int)vehicle.RadioStation;

			//TODO: Set vehicle Extras/Seats/Doors/Windows/Wheels/etc

			return citizenVehicle;
		}
	}
}
