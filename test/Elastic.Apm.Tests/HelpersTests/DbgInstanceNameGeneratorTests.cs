using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class DbgInstanceNameGeneratorTests
	{
		[Fact]
		public void AllDefaultsTest()
		{
			var generator = new DbgInstanceNameGenerator();
			generator.Generate().Should().Be("1");
			generator.Generate().Should().Be("2");
			generator.Generate().Should().Be("3");
		}

		[Fact]
		public void CustomStartTest()
		{
			var generator = new DbgInstanceNameGenerator(23);
			generator.Generate().Should().Be("23");
			generator.Generate().Should().Be("24");
			generator.Generate().Should().Be("25");
		}

		[Fact]
		public void CustomPrefixTest()
		{
			var generator = new DbgInstanceNameGenerator();
			generator.Generate("PrefixA_").Should().Be("PrefixA_1");
			generator.Generate("PrefixB_").Should().Be("PrefixB_2");
			generator.Generate("PrefixC_").Should().Be("PrefixC_3");
		}
	}
}
