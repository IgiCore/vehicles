using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
	public class VehicleWindow : IdentityModel
	{
		public VehicleWindowIndex Index { get; set; }
		public bool IsIntact { get; set; }
		public bool IsRolledDown { get; set; }
	}
}
