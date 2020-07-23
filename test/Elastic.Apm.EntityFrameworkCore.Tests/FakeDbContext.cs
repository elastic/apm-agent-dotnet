// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.EntityFrameworkCore;

namespace Elastic.Apm.EntityFrameworkCore.Tests
{
	public class FakeDbContext : DbContext
	{
		public FakeDbContext(DbContextOptions<FakeDbContext> options)
			: base(options) { }
	}
}
