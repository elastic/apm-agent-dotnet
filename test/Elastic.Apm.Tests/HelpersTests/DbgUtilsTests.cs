using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class DbgUtilsTests
	{
		[Fact]
		public void CurrentMethodName_test() => DbgUtils.CurrentMethodName().Should().Be(nameof(CurrentMethodName_test));
	}
}
