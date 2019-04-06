using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using NFive.SDK.Core.Helpers;
using NFive.SDK.Core.Models;
using NFive.SDK.Core.Models.Audio;

namespace IgiCore.Vehicles.Shared.Models
{
	public class Vehicle : IVehicle
	{
		public Guid Id { get; set; }
		public DateTime Created { get; set; }
		public DateTime? Deleted { get; set; }
		public long Hash { get; set; }
		public int? Handle { get; set; }
		public int? NetId { get; set; }
		public string VIN { get; set; }
		public string LicensePlate { get; set; }
		public Guid? TrackingUserId { get; set; }
		public Position Position { get; set; }
		public float Heading { get; set; }
		public float BodyHealth { get; set; } = 1000;
		public float EngineHealth { get; set; } = 1000;
		public float DirtLevel { get; set; }
		public float FuelLevel { get; set; } = 1000;
		public float OilLevel { get; set; } = 1000;
		public float PetrolTankHealth { get; set; } = 1000;
		public float TowingCraneRaisedAmount { get; set; }
		public bool HasAlarm { get; set; } = true;
		public bool IsAlarmed { get; set; }
		public bool IsAlarmSounding { get; set; }
		public bool HasLock { get; set; } = true;
		public bool IsDrivable { get; set; }
		public bool IsEngineRunning { get; set; } = true;
		public bool HasSeatBelts { get; set; }
		public bool IsHighBeamsOn { get; set; }
		public bool IsLightsOn { get; set; }
		public bool IsInteriorLightOn { get; set; }
		public bool IsSearchLightOn { get; set; }
		public bool IsTaxiLightOn { get; set; }
		public bool IsLeftIndicatorLightOn { get; set; }
		public bool IsRightIndicatorLightOn { get; set; }
		public bool IsFrontBumperBrokenOff { get; set; }
		public bool IsRearBumperBrokenOff { get; set; }
		public bool IsLeftHeadLightBroken { get; set; }
		public bool IsRightHeadLightBroken { get; set; }
		public bool IsRadioEnabled { get; set; }
		public bool IsRoofOpen { get; set; }
		public bool NeedsToBeHotWired { get; set; }
		public bool HasRoof { get; set; } = true;
		public bool IsVehicleConvertible { get; set; }
		public bool CanTiresBurst { get; set; } = true;
		public VehicleColor PrimaryColor { get; set; } = new VehicleColor();
		public VehicleColor SecondaryColor { get; set; } = new VehicleColor();
		public VehicleStockColor PearlescentColor { get; set; }
		public VehicleStockColor DashboardColor { get; set; }
		public VehicleStockColor RimColor { get; set; }
		public Color NeonColor { get; set; } = new Color();
		public VehicleNeonPositions NeonPositions { get; set; }
		public Color TireSmokeColor { get; set; } = new Color();
		public VehicleStockColor TrimColor { get; set; }
		public VehicleWindowTint WindowTint { get; set; } = VehicleWindowTint.None;
		public VehicleLockStatus LockStatus { get; set; } = VehicleLockStatus.None;
		public RadioStation RadioStation { get; set; }
		public VehicleClass Class { get; set; }

		[InverseProperty("Vehicle")]
		public virtual List<VehicleExtra> Extras { get; set; } = new List<VehicleExtra>();

		[InverseProperty("Vehicle")]
		public virtual List<VehicleWindow> Windows { get; set; } = new List<VehicleWindow>();

		[InverseProperty("Vehicle")]
		public virtual List<VehicleSeat> Seats { get; set; } = new List<VehicleSeat>();

		[InverseProperty("Vehicle")]
		public virtual List<VehicleMod> Mods { get; set; } = new List<VehicleMod>();

		[InverseProperty("Vehicle")]
		public virtual List<VehicleDoor> Doors { get; set; } = new List<VehicleDoor>();

		[InverseProperty("Vehicle")]
		public virtual List<VehicleWheel> Wheels { get; set; } = new List<VehicleWheel>();

		public Vehicle() { this.Id = GuidGenerator.GenerateTimeBasedGuid(); }

	}
}
