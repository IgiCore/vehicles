using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace IgiCore.Vehicles.Shared.Models
{
    public class VehicleDoor : IdentityModel
    {
        public VehicleDoorIndex Index { get; set; }
        public bool IsOpen { get; set; } = false;
        public bool IsBroken { get; set; } = false;
		public float Angle { get; set; }

		[Required]
		[ForeignKey("Vehicle")]
		public int VehicleId { get; set; }

		[JsonIgnore]
		public virtual Vehicle Vehicle { get; set; }
	}
}
