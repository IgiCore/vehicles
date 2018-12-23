using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Shared.Models
{
    public class VehicleColor
    {
        public VehicleStockColor StockColor { get; set; }
        public Color CustomColor { get; set; } = new Color();
		public bool IsCustom { get; set; } = false;
    }
}
