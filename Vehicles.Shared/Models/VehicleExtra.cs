using System;
using NFive.SDK.Core.Helpers;
using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
    public class VehicleExtra : IIdentityModel
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Deleted { get; set; }
        public int Index { get; set; }
        public bool IsOn { get; set; }

		public virtual Vehicle Vehicle { get; set; }
		public Guid VehicleId { get; set; }

        public VehicleExtra()
        {
	        this.Id = GuidGenerator.GenerateTimeBasedGuid();
	        this.Created = DateTime.UtcNow;
        }
	}
}
