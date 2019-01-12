namespace IgiCore.Vehicles.Shared.Models
{
    public class Car : Vehicle, ICar
    {
        public ITrailer Trailer { get; set; }
        public IVehicle TowedVehicle { get; set; }
	}
}
