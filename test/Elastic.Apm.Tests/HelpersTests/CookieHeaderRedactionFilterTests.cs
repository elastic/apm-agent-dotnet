// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Filters;
using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests;

public class CookieHeaderRedactionFilterTests
{
	[Theory]
	[MemberData(nameof(TestData))]
	public void RedactsHeadersCorrectly(Dictionary<string, string> headers, IReadOnlyList<WildcardMatcher> sanitizeFieldNames, Dictionary<string, string> expectedHeaders)
	{
		CookieHeaderRedactionFilter.HandleCookieHeader(headers, sanitizeFieldNames);

		if (expectedHeaders is null)
		{
			headers.Should().BeNull();
		}
		else
		{
			headers.Should().Contain(expectedHeaders);
		}
	}

	public static TheoryData<Dictionary<string, string>, IReadOnlyList<WildcardMatcher>, Dictionary<string, string>> TestData() =>
		new()
		{
			// When the input is null, the output should also be null
			{ null, ConfigConsts.DefaultValues.SanitizeFieldNames, null },

			// Standard Cookie header, with a single cookie item whose name does not match the redaction list
			{
				new Dictionary<string, string>() { ["Cookie"] = "Cookie1=Value1" },
				ConfigConsts.DefaultValues.SanitizeFieldNames,
				new Dictionary<string, string>() { ["Cookie"] = "Cookie1=Value1" }
			},

			// Standard Cookie header, with a multiple cookie items whose names do not match the redaction list
			{
				new Dictionary<string, string>() { ["Cookie"] = "Cookie1=Value1; Cookie2=Value2" },
				ConfigConsts.DefaultValues.SanitizeFieldNames,
				new Dictionary<string, string>() { ["Cookie"] = "Cookie1=Value1; Cookie2=Value2" }
			},

			// In the unlikely case that a request includes multiple cookie headers,
			// including some using non-standard casing, we expect each one to be
			// returned.
			{
				new Dictionary<string, string>()
				{
					["Cookie"] = "Cookie1=Value1",
					["cookie"] = "Cookie2=Value2",
					["COOKIE"] = "Cookie3=Value3"
				},
				ConfigConsts.DefaultValues.SanitizeFieldNames,
				new Dictionary<string, string>()
				{
					["Cookie"] = "Cookie1=Value1",
					["cookie"] = "Cookie2=Value2",
					["COOKIE"] = "Cookie3=Value3"
				}
			},

			// If the request contained multiple `Cookie` headers, our code will store each value
			// on the `Context.Request.Headers` dictionary, concatenated by commas.
			{
				new Dictionary<string, string>()
				{
					["Cookie"] = "Cookie1=Value1,Cookie2=Value2"
				},
				ConfigConsts.DefaultValues.SanitizeFieldNames,
				new Dictionary<string, string>()
				{
					["Cookie"] = "Cookie1=Value1,Cookie2=Value2"
				}
			},

			// If the request contained multiple `Cookie` headers, our code will store each value
			// on the `Context.Request.Headers` dictionary, concatenated by commas. In this scenario,
			// one of the headers includes multiple cookie items
			{
				new Dictionary<string, string>()
				{
					["Cookie"] = "Cookie1=Value1,Cookie2=Value2; Cookie3=Value3"
				},
				ConfigConsts.DefaultValues.SanitizeFieldNames,
				new Dictionary<string, string>()
				{
					["Cookie"] = "Cookie1=Value1,Cookie2=Value2; Cookie3=Value3"
				}
			},

			// Standard Cookie header, with a single cookie item whose name matches the redaction list
			{
				new Dictionary<string, string>() { ["Cookie"] = "Authorization=Bearer ABC123" },
				ConfigConsts.DefaultValues.SanitizeFieldNames,
				new Dictionary<string, string>() { ["Cookie"] = "Authorization=[REDACTED]" }
			},

			// Standard Cookie header, with a multiple cookie items, one of which has a name
			// matching the redaction list
			{
				new Dictionary<string, string>() { ["Cookie"] = "Cookie1=Value1; Authorization=Bearer ABC123" },
				ConfigConsts.DefaultValues.SanitizeFieldNames,
				new Dictionary<string, string>() { ["Cookie"] = "Cookie1=Value1; Authorization=[REDACTED]" }
			},

			// If the request contained multiple `Cookie` headers, our code will store each value
			// on the `Context.Request.Headers` dictionary, concatenated by commas. In this scenario,
			// one of the headers includes a cookie item that should be redacted.
			{
				new Dictionary<string, string>() { ["Cookie"] = "Authorization=Bearer ABC123,Cookie1=Value1" },
				ConfigConsts.DefaultValues.SanitizeFieldNames,
				new Dictionary<string, string>() { ["Cookie"] = "Authorization=[REDACTED],Cookie1=Value1" }
			},

			// An example where we have multiple `Cookie` headers, some with multiple items, some of which
			// require redaction.
			{
				new Dictionary<string, string>() { ["Cookie"] = "Cookie1=Value1,Cookie2=Value2; Authorization=Bearer ABC123,Cookie3=Value3; password=thisissecret!" },
				ConfigConsts.DefaultValues.SanitizeFieldNames,
				new Dictionary<string, string>() { ["Cookie"] = "Cookie1=Value1,Cookie2=Value2; Authorization=[REDACTED],Cookie3=Value3; password=[REDACTED]" }
			},

			// MALFORMED EXAMPLES

			// This example includes an invalid cookie entry. We expect to preserve this as-is and cannot redact it.
			{
				new Dictionary<string, string>() { ["Cookie"] = "Cookie1" },
				ConfigConsts.DefaultValues.SanitizeFieldNames,
				new Dictionary<string, string>() { ["Cookie"] = "Cookie1" }
			},

			// This example includes non-standard whitespace. We expect to preserve this as-is.
			{
				new Dictionary<string, string>() { ["Cookie"] = "Cookie1=Value1;     Cookie2=Value2" },
				ConfigConsts.DefaultValues.SanitizeFieldNames,
				new Dictionary<string, string>() { ["Cookie"] = "Cookie1=Value1;     Cookie2=Value2" }
			}
		};
}
