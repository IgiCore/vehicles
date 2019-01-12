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
	}
}
