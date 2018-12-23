using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Client.Extensions
{
	public static class CitizenColorExtensions
	{
		public static Color ToColor(this System.Drawing.Color color) => new Color
		{
			R = color.R,
			G = color.G,
			B = color.B,
			A = color.A,
		};
	}
}
