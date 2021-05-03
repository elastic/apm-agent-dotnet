// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class LibrariesTests
	{
		/// <summary>
		/// Asserts that all types under the Elastic.Apm.Libraries.* namespace are non-public types.
		/// </summary>
		[Fact]
		public void AllTypesAreNonPublic()
		{
			var types = typeof(ApmAgent).Assembly.GetTypes()
				.Where(t => t.Namespace != null && t.Namespace.StartsWith("Elastic.Apm.Libraries", StringComparison.Ordinal))
				.ToArray();

			types.Should().NotBeNullOrEmpty();

			foreach (var type in types)
			{
				if (!type.IsNested)
					type.IsNotPublic.Should().BeTrue($"{type.FullName} is expected to be a non-public type");
			}
		}
	}
}
