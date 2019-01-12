using IgiCore.Characters.Shared.Models;
using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
    public class VehicleSeat : IdentityModel
	{
        public VehicleSeatIndex Index { get; set; }
        public ICharacter Character { get; set; }
	}
}
