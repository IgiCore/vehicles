using System;
using NFive.SDK.Core.Helpers;
using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
	public class VehicleWindow : IIdentityModel
	{
		public Guid Id { get; set; }
		public DateTime Created { get; set; }
		public DateTime? Deleted { get; set; }
		public VehicleWindowIndex Index { get; set; }
		public bool IsIntact { get; set; }
		public bool IsRolledDown { get; set; }

		public VehicleWindow()
		{
			this.Id = GuidGenerator.GenerateTimeBasedGuid();
			this.Created = DateTime.UtcNow;
		}
	}
}
