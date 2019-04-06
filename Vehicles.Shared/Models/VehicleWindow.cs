using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
	public class VehicleWindow : IdentityModel
	{
		public VehicleWindowIndex Index { get; set; }
		public bool IsIntact { get; set; }
		public bool IsRolledDown { get; set; }

		[Required]
		[ForeignKey("Vehicle")]
		public int VehicleId { get; set; }

		[JsonIgnore]
		public virtual Vehicle Vehicle { get; set; }
	}
}
