using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
    public class VehicleExtra : IdentityModel
	{
        public int Index { get; set; }
        public bool IsOn { get; set; }

		[Required]
		[ForeignKey("Vehicle")]
		public int VehicleId { get; set; }

		[JsonIgnore]
		public virtual Vehicle Vehicle { get; set; }
	}
}
