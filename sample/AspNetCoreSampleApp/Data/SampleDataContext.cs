using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSampleApp.Data
{
	public class SampleDataContext : IdentityDbContext
	{
		public SampleDataContext(DbContextOptions dbContextOptions) : base(dbContextOptions) { }

		public DbSet<SampleData> SampleTable { get; set; }
	}
}
