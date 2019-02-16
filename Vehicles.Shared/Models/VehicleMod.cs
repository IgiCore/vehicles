﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
    public class VehicleMod : IdentityModel
	{
        public int Index { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public int Count { get; set; }
        public VehicleModType Type { get; set; }

        [Required]
        [ForeignKey("Vehicle")]
        public Guid VehicleId { get; set; }

        [JsonIgnore]
        public virtual Vehicle Vehicle { get; set; }
	}
}
