// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#if !NETFRAMEWORK
using System;
#endif
using System.Collections.Generic;

namespace Elastic.Apm.Helpers;

internal static class CookieHeaderParser
{
	public static Dictionary<string, string> ParseCookies(string cookieHeader)
	{
		// Implementation notes:
		// This method handles a cookie header value for both ASP.NET (classic) and
		// ASP.NET Core. As a result it must handle two possible formats. In ASP.NET
		// (classic) the string is the actual Cookie value as sent over the wire, conforming
		// to the HTTP standards. This uses the semicolon separator and a space between
		// entries. For ASP.NET Core, when we parse the headers, we convert from the
		// StringValues by calling ToString. This results in each entry being separated
		// by a regular colon and no space.

		if (string.IsNullOrEmpty(cookieHeader))
			return null;

		var cookies = new Dictionary<string, string>();

#if NETFRAMEWORK
		// The use of `Span<T>` in NETFX can cause runtime assembly loading issues due to our friend,
		// binding redirects. For now, we take a slightly less allocation efficient route here, rather
		// than risk introducing runtime issues for consumers. Technically, this should be "fixed" in
		// NET472+, but during testing I surprisingly still reproduced an exception. Elastic.APM depends on
		// `System.Diagnostics.DiagnosticSource 5.0.0` which itself depends on `System.Runtime.CompilerServices.Unsafe`
		// which is where the exception occurs. 5.0.0 is marked as deprecated so we could look to prefer
		// a new version but we have special handling for the ElasticApmAgentStartupHook
		// zip file version. For now, we decided not to mess with this as it's hard to test all scenarios.

		var cookieValues = cookieHeader.Split(',', ';');

		foreach (var cookieValue in cookieValues)
		{
			var trimmed = cookieValue.Trim();
			var parts = trimmed.Split('=');

			// Fow now, we store only the first value for a given key. This aligns to our nodeJS agent behavior.
			if (parts.Length == 2 
				&& !string.IsNullOrEmpty(parts[0]) 
				&& !cookies.ContainsKey(parts[0])
				&& !string.IsNullOrEmpty(parts[1]))
			{
				cookies.Add(parts[0], parts[1]);
			}
		}

		return cookies;
#else
		var span = cookieHeader.AsSpan();

		while (span.Length > 0)
		{
			var foundComma = true;
			var separatorIndex = span.IndexOfAny(',', ';');

			if (separatorIndex == -1)
			{
				foundComma = false;
				separatorIndex = span.Length;
			}

			var entry = span.Slice(0, separatorIndex);

			var equalsIndex = entry.IndexOf('=');

			if (equalsIndex > -1)
			{
				var key = entry.Slice(0, equalsIndex);
				var value = entry.Slice(equalsIndex + 1);

				var keyString = key.ToString();
				var valueString = value.ToString();

				// Fow now, we store only the first value for a given key. This aligns to our nodeJS agent behavior.
#if NETSTANDARD2_0
				if (!string.IsNullOrEmpty(keyString) && !cookies.ContainsKey(keyString) && !string.IsNullOrEmpty(valueString))
					cookies.Add(keyString, valueString);
#else
				if (!string.IsNullOrEmpty(keyString) && !string.IsNullOrEmpty(valueString))
					cookies.TryAdd(keyString, valueString);
#endif
			}

			span = span.Slice(foundComma ? separatorIndex + 1 : span.Length);

			// skip any white space between the separator and the next entry
			while (span.Length > 0)
			{
				if (span[0] != ' ')
					break;

				span = span.Slice(1);
			}
		}

		return cookies;
#endif
	}
}

