// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests;

public class CookieHeaderParserTests
{
	[Theory]
	[MemberData(nameof(TestData))]
	public void ParsesHeadersCorrectly(string input, Dictionary<string, string> expected)
	{
		var result = Helpers.CookieHeaderParser.ParseCookies(input);

		if (expected is null)
		{
			result.Should().BeNull();
			return;
		}

		result.Count.Should().Be(expected.Count);
		result.Should().Contain(expected);
	}

	public static IEnumerable<object[]> TestData() => [
		[null, null],
		["", null],
		["Key1=Value1", new Dictionary<string, string>() { { "Key1", "Value1" }}],
		[ "Key1=Value1,Key2=Value2", new Dictionary<string, string>()
		{
			{ "Key1", "Value1" },
			{ "Key2", "Value2" }
		}],
		[ "Key1=Value1,Key2=Value2,Key3=Value3", new Dictionary<string, string>()
		{
			{ "Key1", "Value1" },
			{ "Key2", "Value2" },
			{ "Key3", "Value3" }
		}],
		[ "Key1=Value1; Key2=Value2", new Dictionary<string, string>()
		{
			{ "Key1", "Value1" },
			{ "Key2", "Value2" }
		}],
		[ "Key1=Value1; Key2=Value2; Key3=Value3", new Dictionary<string, string>()
		{
			{ "Key1", "Value1" },
			{ "Key2", "Value2" },
			{ "Key3", "Value3" }
		}],
		[ "Key1=Value1; Key2=Value2; Key1=Value3", new Dictionary<string, string>()
		{
			{ "Key1", "Value1" },
			{ "Key2", "Value2" }
		}],
	];
}
