using System;
using System.Text;
using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class StringExtensionsTests
	{
		[Fact]
		public void IsEmptyTest()
		{
			"".IsEmpty().Should().BeTrue();
			string.Empty.IsEmpty().Should().BeTrue();

			"abc".IsEmpty().Should().BeFalse();
			" ".IsEmpty().Should().BeFalse();

			((Action)(() => ((string) null).IsEmpty())).Should().ThrowExactly<ArgumentNullException>();
		}

		[Fact]
		public void RepeatTest()
		{
			"abc".Repeat(3).Should().Be("abcabcabc");
			" ".Repeat(2).Should().Be("  ");

			"abc".Repeat(1).Should().Be("abc");

			"xyz".Repeat(0).Should().BeEmpty();
			string.Empty.Repeat(10).Should().BeEmpty();

			((Action)(() => ((string) null).Repeat(1))).Should().ThrowExactly<ArgumentNullException>();
			((Action)(() => ((string) null).Repeat(0))).Should().ThrowExactly<ArgumentNullException>();
			((Action)(() => ((string) null).Repeat(-1))).Should().ThrowExactly<ArgumentNullException>();
			((Action)(() => "abc".Repeat(-321))).Should().ThrowExactly<ArgumentException>().WithMessage("*-321*");
		}
	}
}
