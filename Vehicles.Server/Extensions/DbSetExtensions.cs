using System.Data.Entity;
using System.Linq;
using NFive.SDK.Core.Models;

namespace IgiCore.Vehicles.Server.Extensions
{
	public static class DbSetExtensions
	{
		public static IQueryable<T> NotDeleted<T>(this DbSet<T> t) where T : IdentityModel
		{
			return t.Where(i => i.Deleted == null);
		}
	}
}
