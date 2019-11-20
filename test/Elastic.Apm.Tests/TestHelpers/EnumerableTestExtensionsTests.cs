using System;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class EnumerableTestExtensionsTests
	{
		[Fact]
		public void ZipWithIndex_test()
		{
			Array.Empty<string>().ZipWithIndex().Should().BeEmpty();

			Array.Empty<string>().ZipWithIndex(123).Should().BeEmpty();

			new[] { "zero" }.ZipWithIndex().Should().BeEquivalentTo(new[] { (0, "zero") });

			new[] { "one" }.ZipWithIndex(1).Should().BeEquivalentTo(new[] { (1, "one") });

			new[] { "zero", "one", "two" }.ZipWithIndex().Should().BeEquivalentTo(new[] { (0, "zero"), (1, "one"), (2, "two") });

			new[] { "one", "two", "three" }.ZipWithIndex(1).Should().BeEquivalentTo(new[] { (1, "one"), (2, "two"), (3, "three") });

			new[] { "int.MaxValue" }.ZipWithIndex(int.MaxValue).Should().BeEquivalentTo(new[] { (int.MaxValue, "int.MaxValue") });

			new[] { "int.MaxValue - 1", "int.MaxValue" }.ZipWithIndex(int.MaxValue - 1)
				.Should()
				.BeEquivalentTo(
					new[] { (int.MaxValue - 1, "int.MaxValue - 1"), (int.MaxValue, "int.MaxValue") });
		}
	}
}
