using System;
using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Tests.TestHelpers.FluentAssertionsUtils;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class ContractExtensionsTests
	{
		[Fact]
		public void ThrowIfArgumentNullTest()
		{
			string stringArg1 = null;
			// ReSharper disable once ExpressionIsAlwaysNull
			AsAction(() => stringArg1.ThrowIfArgumentNull(nameof(stringArg1)))
				.Should()
				.ThrowExactly<ArgumentNullException>()
				.WithMessage($"*{nameof(stringArg1)}*");

			var stringArg2 = string.Empty;
			stringArg2.ThrowIfArgumentNull(nameof(stringArg2)).Should().Be(string.Empty);

			var stringArg3 = "some non-empty string";
			stringArg3.ThrowIfArgumentNull(nameof(stringArg3)).Should().Be("some non-empty string");
		}

		[Fact]
		public void ThrowIfNullableValueArgumentNullTest()
		{
			int? nullableIntArg1 = null;
			// ReSharper disable once ExpressionIsAlwaysNull
			AsAction(() => nullableIntArg1.ThrowIfNullableValueArgumentNull(nameof(nullableIntArg1)))
				.Should()
				.ThrowExactly<ArgumentNullException>()
				.WithMessage($"*{nameof(nullableIntArg1)}*");

			int? nullableIntArg2 = 0;
			nullableIntArg2.ThrowIfNullableValueArgumentNull(nameof(nullableIntArg2)).Should().Be(0);

			int? nullableIntArg3 = 12345;
			nullableIntArg3.ThrowIfNullableValueArgumentNull(nameof(nullableIntArg3)).Should().Be(12345);
		}

		[Fact]
		public void ThrowIfArgumentNegativeTest()
		{
			var intArg1 = -1;
			AsAction(() => intArg1.ThrowIfArgumentNegative(nameof(intArg1)))
				.Should()
				.ThrowExactly<ArgumentException>()
				.WithMessage($"*{nameof(intArg1)}*-1*");

			var intArg2 = -9876;
			AsAction(() => intArg2.ThrowIfArgumentNegative(nameof(intArg2)))
				.Should()
				.ThrowExactly<ArgumentException>()
				.WithMessage($"*{nameof(intArg2)}*-9876*");

			var intArg3 = 0;
			intArg3.ThrowIfArgumentNegative(nameof(intArg3)).Should().Be(0);

			var intArg4 = 12345;
			intArg4.ThrowIfArgumentNegative(nameof(intArg4)).Should().Be(12345);
		}
	}
}
