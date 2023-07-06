// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Elastic.Apm.EntityFrameworkCore.Tests
{
	public class FakeDbContext : DbContext
	{
		// Data structure to allow running EFCore linq queries; requires running EnsureCreated() inside tests
		public virtual DbSet<FakeData> Data { get; set; }

		public FakeDbContext(DbContextOptions<FakeDbContext> options)
			: base(options) { }
	}

	/// <summary>
	/// Data structure to allow running EFCore linq queries; requires running EnsureCreated() inside tests
	/// </summary>
	public class FakeData
	{
		[Key]
		public Guid Id { get; set; }
	}
}
