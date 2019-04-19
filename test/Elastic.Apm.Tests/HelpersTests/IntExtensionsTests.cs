using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class IntExtensionsTests
	{
		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(31)]
		public void TestRepeat(int repeatCount)
		{
			var counter = 0;
			repeatCount.Repeat(() => { ++counter; });
			counter.Should().Be(repeatCount);

			counter = 0;
			repeatCount.Repeat(i =>
			{
				i.Should().Be(counter);
				++counter;
			});
			counter.Should().Be(repeatCount);
		}
	}
}
