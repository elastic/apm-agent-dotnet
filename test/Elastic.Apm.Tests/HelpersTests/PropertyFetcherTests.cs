using Elastic.Apm.Helpers;
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
