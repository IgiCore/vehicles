﻿using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
    public class VehicleWheel : IdentityModel
	{
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
	}
}
