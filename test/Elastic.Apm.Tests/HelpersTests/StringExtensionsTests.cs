// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Tests.TestHelpers.FluentAssertionsUtils;

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

			AsAction(() => ((string)null).IsEmpty()).Should().ThrowExactly<ArgumentNullException>();
		}

		[Fact]
		public void RepeatTest()
		{
			"abc".Repeat(3).Should().Be("abcabcabc");
			" ".Repeat(2).Should().Be("  ");

			"abc".Repeat(1).Should().Be("abc");

			"xyz".Repeat(0).Should().BeEmpty();
			string.Empty.Repeat(10).Should().BeEmpty();

			AsAction(() => ((string)null).Repeat(1)).Should().ThrowExactly<ArgumentNullException>();
			AsAction(() => ((string)null).Repeat(0)).Should().ThrowExactly<ArgumentNullException>();
			AsAction(() => ((string)null).Repeat(-1)).Should().ThrowExactly<ArgumentNullException>();
			AsAction(() => "abc".Repeat(-321)).Should().ThrowExactly<ArgumentException>().WithMessage("*-321*");
		}

		[Theory]
		[InlineData("", "", true)]
		[InlineData("a", "", true)]
		[InlineData("abc", "A", true)]
		[InlineData("abc", "B", true)]
		[InlineData("abc", "C", true)]
		[InlineData("abc", "AB", true)]
		[InlineData("abc", "BC", true)]
		[InlineData("abc", "CC", false)]
		[InlineData("", "A", false)]
		public void ContainsOrdinalIgnoreCase_test(string text, string subStr, bool expectedResult) =>
			text.ContainsOrdinalIgnoreCase(subStr).Should().Be(expectedResult);
	}
}
