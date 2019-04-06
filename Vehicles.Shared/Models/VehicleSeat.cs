using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IgiCore.Characters.Shared.Models;
using Newtonsoft.Json;
using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
    public class VehicleSeat : IdentityModel
	{
        public VehicleSeatIndex Index { get; set; }
        public ICharacter Character { get; set; }

        [Required]
        [ForeignKey("Vehicle")]
        public int VehicleId { get; set; }

        [JsonIgnore]
        public virtual Vehicle Vehicle { get; set; }
	}
}
