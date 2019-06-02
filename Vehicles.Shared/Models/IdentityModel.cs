using System;

namespace IgiCore.Vehicles.Shared.Models
{
	public class IdentityModel
	{
		public int Id { get; set; }
		public DateTime Created { get; set; }
		public DateTime? Deleted { get; set; }
	}
}
