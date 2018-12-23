using System.Data.Entity;
using IgiCore.Vehicles.Shared.Models;
using NFive.SDK.Core.Models.Player;
using NFive.SDK.Server.Storage;

namespace IgiCore.Vehicles.Server.Storage
{
	public class StorageContext : EFContext<StorageContext>
	{
		public DbSet<Vehicle> Vehicles { get; set; }
		public DbSet<VehicleExtra> VehicleExtras { get; set; }
		public DbSet<VehicleWheel> VehicleWheels { get; set; }
		public DbSet<VehicleWindow> VehicleWindows { get; set; }
		public DbSet<VehicleMod> VehicleMods { get; set; }
		public DbSet<VehicleDoor> VehicleDoors { get; set; }
		public DbSet<VehicleSeat> VehicleSeats { get; set; }

		public DbSet<Car> Cars { get; set; }
		public DbSet<Bike> Bikes { get; set; }
	}
}
