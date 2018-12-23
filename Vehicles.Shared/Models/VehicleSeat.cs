using System;
using IgiCore.Characters.Shared.Models;
using NFive.SDK.Core.Helpers;
using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
    public class VehicleSeat : IIdentityModel
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Deleted { get; set; }
        public VehicleSeatIndex Index { get; set; }
        public ICharacter Character { get; set; }

        public VehicleSeat()
        {
	        this.Id = GuidGenerator.GenerateTimeBasedGuid();
	        this.Created = DateTime.UtcNow;
        }
	}
}
