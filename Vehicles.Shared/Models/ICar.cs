namespace IgiCore.Vehicles.Shared.Models
{
    public interface ICar : IRoadVehicle
    {
        ITrailer Trailer { get; set; }
        IVehicle TowedVehicle { get; set; }
    }
}
