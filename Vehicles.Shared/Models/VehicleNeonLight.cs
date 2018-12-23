using System;

namespace IgiCore.Vehicles.Shared.Models
{
	[Flags] public enum VehicleNeonPositions
	{
		None = 0,
		Left = 1,
		Right = 2,
		Front = 4,
		Back = 8
	}
}
