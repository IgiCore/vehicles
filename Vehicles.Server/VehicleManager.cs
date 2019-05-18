using System.Collections.Generic;
using System.Linq;
using IgiCore.Vehicles.Server.Storage;
using IgiCore.Vehicles.Shared;
using IgiCore.Vehicles.Shared.Models;
using NFive.SDK.Server.Events;
using NFive.SDK.Server.Rpc;

namespace IgiCore.Vehicles.Server
{
	public class VehicleManager
	{
		/// <summary>
		/// The controller event manager.
		/// </summary>
		protected readonly IEventManager Events;

		/// <summary>
		/// The controller RPC handler.
		/// </summary>
		protected readonly IRpcHandler Rpc;

		public List<Vehicle> ActiveVehicles => this.Events.Request<List<Vehicle>>(VehicleEvents.GetActiveVehicles);
	}
}
