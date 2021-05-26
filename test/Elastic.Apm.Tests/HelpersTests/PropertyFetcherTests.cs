// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Helpers;
using Elastic.Apm.Reflection;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class CascadeFetcherTests
	{
		[Fact]
		public void Fetch_ShouldCorrectlyFetchValue_WhenInnerFetcherIsPropertyFetcher()
		{
			// Arrange
			const string commandText = "command text";
			var fetcher = new CascadePropertyFetcher(new PropertyFetcher("Command"), "CommandText");

			// Act
			var result = fetcher.Fetch(new { Command = new { CommandText = commandText } });

			// Assert
			result.Should().Be(commandText);
		}

		[Fact]
		public void Fetch_ShouldCorrectlyFetchValue_WhenInnerFetcherIsCascadePropertyFetcher()
		{
			// Arrange
			const string database = "database name";
			var fetcher = new CascadePropertyFetcher(new CascadePropertyFetcher(new PropertyFetcher("Command"), "Connection"), "Database");

			// Act
			var result = fetcher.Fetch(new { Command = new { Connection = new { Database = database } } });

			// Assert
			result.Should().Be(database);
		}
	}
}
