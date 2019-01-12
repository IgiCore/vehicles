using System;
using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
    public class VehicleExtra : IdentityModel
	{
        public int Index { get; set; }
        public bool IsOn { get; set; }

		public virtual Vehicle Vehicle { get; set; }
		public Guid VehicleId { get; set; }
	}
}
