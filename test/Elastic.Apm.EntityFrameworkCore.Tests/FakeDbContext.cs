using Microsoft.EntityFrameworkCore;

namespace Elastic.Apm.EntityFrameworkCore.Tests
{
	public class FakeDbContext : DbContext
	{
		public FakeDbContext(DbContextOptions<FakeDbContext> options)
			: base(options) { }
	}
}
