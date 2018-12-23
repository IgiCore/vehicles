using System;
using NFive.SDK.Core.Helpers;
using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
    public class VehicleDoor : IIdentityModel
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Deleted { get; set; }
        public VehicleDoorIndex Index { get; set; }
        public bool IsOpen { get; set; } = false;
        public bool IsBroken { get; set; } = false;
		public float Angle { get; set; }

		public VehicleDoor()
		{
			this.Id = GuidGenerator.GenerateTimeBasedGuid();
			this.Created = DateTime.UtcNow;
		}
    }
}
