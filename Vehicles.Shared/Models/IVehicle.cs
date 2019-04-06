using System;
using System.Collections.Generic;
using IgiCore.Tracking.Shared.Models;
using NFive.SDK.Core.Models;
using NFive.SDK.Core.Models.Audio;

namespace IgiCore.Vehicles.Shared.Models
{
    public interface IVehicle
    {
	    int Id { get; set; }
	    int? Handle { get; set; }
	    Guid TrackingUserId { get; set; }
	    int? NetId { get; set; }
	    DateTime Created { get; set; }
	    DateTime? Deleted { get; set; }
		long Hash { get; set; }
	    Position Position { get; set; }
	    float Heading { get; set; }
		string VIN { get; set; }
	    string LicensePlate { get; set; }
		float BodyHealth { get; set; }
        float EngineHealth { get; set; }
        float DirtLevel { get; set; }
        float FuelLevel { get; set; }
        float OilLevel { get; set; }
        float PetrolTankHealth { get; set; }
        float TowingCraneRaisedAmount { get; set; }
        bool HasAlarm { get; set; }
        bool IsAlarmed { get; set; }
	    bool IsAlarmSounding { get; set; }
		bool HasLock { get; set; }
        bool IsDrivable { get; set; }
        bool IsEngineRunning { get; set; }
        bool HasSeatBelts { get; set; }
        bool IsHighBeamsOn { get; set; }
        bool IsLightsOn { get; set; }
        bool IsInteriorLightOn { get; set; }
        bool IsSearchLightOn { get; set; }
        bool IsTaxiLightOn { get; set; }
        bool IsLeftIndicatorLightOn { get; set; }
        bool IsRightIndicatorLightOn { get; set; }
        bool IsFrontBumperBrokenOff { get; set; }
        bool IsRearBumperBrokenOff { get; set; }
        bool IsLeftHeadLightBroken { get; set; }
        bool IsRightHeadLightBroken { get; set; }
        bool IsRadioEnabled { get; set; }
        bool IsRoofOpen { get; set; }
        bool NeedsToBeHotWired { get; set; }
        bool CanTiresBurst { get; set; }
        bool HasRoof { get; set; }
        bool IsVehicleConvertible { get; set; }
        VehicleColor PrimaryColor { get; set; }
        VehicleColor SecondaryColor { get; set; }
        VehicleStockColor PearlescentColor { get; set; }
	    VehicleStockColor DashboardColor { get; set; }
	    VehicleStockColor RimColor { get; set; }
        Color NeonColor { get; set; }
	    VehicleNeonPositions NeonPositions { get; set; }
		Color TireSmokeColor { get; set; }
	    VehicleStockColor TrimColor { get; set; }
        VehicleWindowTint WindowTint { get; set; }
        VehicleLockStatus LockStatus { get; set; }
        RadioStation RadioStation { get; set; }
        VehicleClass Class { get; set; }
        List<VehicleExtra> Extras { get; set; }
        List<VehicleWindow> Windows { get; set; }
        List<VehicleSeat> Seats { get; set; }
        List<VehicleMod> Mods { get; set; }
        List<VehicleDoor> Doors { get; set; }
        List<VehicleWheel> Wheels { get; set; }
    }
}
