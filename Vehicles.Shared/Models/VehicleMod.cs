using System;
using NFive.SDK.Core.Helpers;
using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
    public class VehicleMod : IIdentityModel
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Deleted { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public int Count { get; set; }
        public VehicleModType Type { get; set; }

        public VehicleMod()
        {
	        this.Id = GuidGenerator.GenerateTimeBasedGuid();
	        this.Created = DateTime.UtcNow;
        }
	}
}
