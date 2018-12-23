using System;
using NFive.SDK.Core.Helpers;
using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
    public class VehicleWheel : IIdentityModel
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Deleted { get; set; }
        public VehicleWheelType Type { get; set; }
		/// <summary>
		/// Gets or sets the Position.
		/// </summary>
		/// <value>
		/// Wheel index in VehicleWheelCollection
		/// </value>
		public VehicleWheelPosition Position { get; set; }
		/// <summary>
		/// Gets or sets the Index.
		/// </summary>
		/// <value>
		/// Wheel index from the type set.
		/// </value>
		public int Index { get; set; }
        public bool IsBurst { get; set; }

        public VehicleWheel()
        {
	        this.Id = GuidGenerator.GenerateTimeBasedGuid();
	        this.Created = DateTime.UtcNow;
        }
	}
}
