using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class DbgUtilsTests
	{
		[Fact]
		public void GetCurrentMethodName_test()
		{
			DbgUtils.GetCurrentMethodName().Should().Be(nameof(GetCurrentMethodName_test));
		}
	}
}
