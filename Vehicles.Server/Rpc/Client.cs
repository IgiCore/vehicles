using System.Globalization;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using NFive.SDK.Server.Rpc;

namespace IgiCore.Vehicles.Server.Rpc
{
	public class Client : IClient
	{
		public IRpcTrigger Event(string @event)
		{
			throw new System.NotImplementedException();
		}

		public int Handle { get; }

		public string Name { get; }

		public string License { get; }

		public long? SteamId { get; }

		public string EndPoint { get; }
		public int Ping { get; }

		public Client(int handle)
		{
			this.Handle = handle;

			var player = new PlayerList()[this.Handle];

			this.Name = player.Name;
			this.License = player.Identifiers["license"];
			this.SteamId = player.Identifiers.Contains("steam") ? long.Parse(player.Identifiers["steam"], NumberStyles.HexNumber) : default(long?);
			this.EndPoint = player.EndPoint;
		}
	}
}
