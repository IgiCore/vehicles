using System;
using NFive.SDK.Core.Controllers;

namespace IgiCore.Vehicles.Shared
{
	public class Configuration : ControllerConfiguration
	{
		public ushort DespawnDistance { get; set; } = 500;
		public TimeSpan SpawnPollRate { get; set; } = TimeSpan.FromSeconds(5);
		public TimeSpan TrackingPollRate { get; set; } = TimeSpan.FromSeconds(5);
		public TimeSpan AutosaveRate { get; set; } = TimeSpan.FromSeconds(5);
	}
}
