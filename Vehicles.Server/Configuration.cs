using System;
using NFive.SDK.Core.Controllers;

namespace IgiCore.Vehicles.Server
{
	public class Configuration : ControllerConfiguration
	{
		public ushort DespawnDistance { get; set; } = 500;
		public TimeSpan SpawnPollRate { get; set; } = TimeSpan.FromSeconds(30);
	}
}
