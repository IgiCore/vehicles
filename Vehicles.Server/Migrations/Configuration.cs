using JetBrains.Annotations;
using NFive.SDK.Server.Migrations;
using IgiCore.Vehicles.Server.Storage;

namespace IgiCore.Vehicles.Server.Migrations
{
	[UsedImplicitly]
	public sealed class Configuration : MigrationConfiguration<StorageContext> { }
}
