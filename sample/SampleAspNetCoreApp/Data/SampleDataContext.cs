// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SampleAspNetCoreApp.Data
{
	public class SampleDataContext : IdentityDbContext
	{
		public SampleDataContext(DbContextOptions dbContextOptions) : base(dbContextOptions) { }

		public DbSet<SampleData> SampleTable { get; set; }
	}
}
