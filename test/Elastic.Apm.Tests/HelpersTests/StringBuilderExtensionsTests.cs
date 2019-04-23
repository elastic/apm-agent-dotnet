using System.Text;
using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class StringBuilderExtensionsTests
	{
		[Fact]
		public void TestIsEmpty()
		{
			new StringBuilder().IsEmpty().Should().BeTrue();
			new StringBuilder().Append("").IsEmpty().Should().BeTrue();
			new StringBuilder().Append("abc").IsEmpty().Should().BeFalse();
			new StringBuilder().Append("abc").Clear().IsEmpty().Should().BeTrue();
		}
		[Fact]
		public void TestAppendSeparatedIfNotEmpty()
		{
			new StringBuilder().AppendSeparatedIfNotEmpty("_", "abc").ToString().Should().Be("abc");
			new StringBuilder().Append("abc").AppendSeparatedIfNotEmpty("_", "def").ToString().Should().Be("abc_def");
		}
	}
}
