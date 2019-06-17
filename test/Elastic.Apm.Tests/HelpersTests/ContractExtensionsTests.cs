using System;
using System.Text;
using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class ContractExtensionsTests
	{
		[Fact]
		public void ThrowIfArgumentNullTest()
		{
			string stringArg = null;
			((Action)(() => stringArg.ThrowIfArgumentNull(nameof(stringArg)))).
				Should().ThrowExactly<ArgumentNullException>().WithMessage($"*{nameof(stringArg)}*");

			stringArg = string.Empty;
			stringArg.ThrowIfArgumentNull(nameof(stringArg)).Should().Be(string.Empty);

			stringArg = "some non-empty string";
			stringArg.ThrowIfArgumentNull(nameof(stringArg)).Should().Be("some non-empty string");
		}

		[Fact]
		public void ThrowIfNullableValueArgumentNullTest()
		{
			int? nullableIntArg = null;
			((Action)(() => nullableIntArg.ThrowIfNullableValueArgumentNull(nameof(nullableIntArg)))).
				Should().ThrowExactly<ArgumentNullException>().WithMessage($"*{nameof(nullableIntArg)}*");

			nullableIntArg = 0;
			nullableIntArg.ThrowIfNullableValueArgumentNull(nameof(nullableIntArg)).Should().Be(0);

			nullableIntArg = 12345;
			nullableIntArg.ThrowIfNullableValueArgumentNull(nameof(nullableIntArg)).Should().Be(12345);
		}

		[Fact]
		public void ThrowIfArgumentNegativeTest()
		{
			int intArg = -1;
			((Action)(() => intArg.ThrowIfArgumentNegative(nameof(intArg)))).
				Should().ThrowExactly<ArgumentException>().WithMessage($"*{nameof(intArg)}*-1*");

			intArg = -9876;
			((Action)(() => intArg.ThrowIfArgumentNegative(nameof(intArg)))).
				Should().ThrowExactly<ArgumentException>().WithMessage($"*{nameof(intArg)}*-9876*");

			intArg = 0;
			intArg.ThrowIfArgumentNegative(nameof(intArg)).Should().Be(0);

			intArg = 12345;
			intArg.ThrowIfArgumentNegative(nameof(intArg)).Should().Be(12345);
		}
	}
}
