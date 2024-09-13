// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#if !NETFRAMEWORK
using System;
using System.Buffers;
#endif
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace Elastic.Apm.Helpers;

internal static class CookieHeaderRedacter
{
#if !NETFRAMEWORK
	private static ReadOnlySpan<char> Redacted => Consts.Redacted.AsSpan();
	private static ReadOnlySpan<char> SemiColon => ";".AsSpan();
	private static ReadOnlySpan<char> Comma => ",".AsSpan();
#endif

	public static string Redact(string cookieHeaderValue, IReadOnlyList<WildcardMatcher> namesToSanitize)
	{
		// Implementation notes:
		// This method handles a cookie header value for both ASP.NET (classic) and
		// ASP.NET Core. As a result it must handle two possible formats. In ASP.NET
		// (classic) the string is the actual Cookie value as sent over the wire, conforming
		// to the HTTP standards. This uses the semicolon separator and a space between
		// entries. For ASP.NET Core, when we parse the headers, we convert from the
		// StringValues by calling ToString. This results in each entry being separated
		// by a regular colon and no space.

		// When the headers are stored into the `Request.Headers` on our APM data model,
		// multiple headers of the same name, are concatenated into a comma-separated list.
		// When redacting, we preserve this separation.

		if (string.IsNullOrEmpty(cookieHeaderValue))
			return null;

		var redactedCookieHeader = string.Empty;

#if NETFRAMEWORK
		// The use of `Span<T>` in NETFX can cause runtime assembly loading issues due to our friend,
		// binding redirects. For now, we take a slightly less allocation efficient route here, rather
		// than risk introducing runtime issues for consumers. Technically, this should be "fixed" in
		// NET472+, but during testing I surprisingly still reproduced an exception. Elastic.APM depends on
		// `System.Diagnostics.DiagnosticSource 5.0.0` which itself depends on `System.Runtime.CompilerServices.Unsafe`
		// which is where the exception occurs. 5.0.0 is marked as deprecated so we could look to prefer
		// a new version but we have special handling for the ElasticApmAgentStartupHook
		// zip file version. For now, we decided not to mess with this as it's hard to test all scenarios.

		var sb = new StringBuilder(cookieHeaderValue.Length);
		var cookieHeaderEntries = cookieHeaderValue.Split(',');

		var requiresComma = false;
		foreach (var entry in cookieHeaderEntries)
		{
			if (requiresComma)
				sb.Append(',');

			var cookieValues = entry.Split(';');

			var requiresSemiColon = false;
			foreach (var cookieValue in cookieValues)
			{
				var parts = cookieValue.Split('=');

				if (requiresSemiColon)
					sb.Append(';');

				if (parts.Length == 1)
				{
					sb.Append(parts[0]);
				}
				else
				{
					if (WildcardMatcher.IsAnyMatch(namesToSanitize, parts[0].Trim()))
					{
						sb.Append(parts[0]);
						sb.Append('=');
						sb.Append(Consts.Redacted);
					}
					else
					{
						sb.Append(cookieValue);
					}
				}

				requiresSemiColon = true;
			}

			requiresComma = true;
		}

		redactedCookieHeader = sb.ToString();
		return redactedCookieHeader;
#else
		var cookieHeaderValueSpan = cookieHeaderValue.AsSpan();

		var written = 0;
		var redactedBufferArray = ArrayPool<char>.Shared.Rent(cookieHeaderValueSpan.Length);
		var redactedBuffer = redactedBufferArray.AsSpan();

		var whitespaceCount = 0;
		while (cookieHeaderValueSpan.Length > 0)
		{
			// We first split on commas, to handle cases where we've combined,
			// multiple headers of the same key into a concatened string.
			var foundComma = true;
			var commaIndex = cookieHeaderValueSpan.IndexOf(',');

			if (commaIndex == -1) // If there are no more separators,
			{
				foundComma = false;
				commaIndex = cookieHeaderValueSpan.Length;
			}

			var cookieHeaderEntrySpan = cookieHeaderValueSpan.Slice(0, commaIndex);

			if (written > 0)
			{
				Comma.CopyTo(redactedBuffer.Slice(written));
				written += Comma.Length;
			}

			var writeSemiColon = false;
			// Next handle each cookie item in the cookie header value
			while (cookieHeaderEntrySpan.Length > 0)
			{
				var foundSemiColon = true;
				var semiColonIndex = cookieHeaderEntrySpan.IndexOf(';');

				if (semiColonIndex == -1) // If there are no more separators,
				{
					foundSemiColon = false;
					semiColonIndex = cookieHeaderEntrySpan.Length;
				}

				var cookieItem = cookieHeaderEntrySpan.Slice(0, semiColonIndex);

				var equalsIndex = cookieItem.IndexOf('=');

				if (equalsIndex > -1)
				{
					if (writeSemiColon)
					{
						SemiColon.CopyTo(redactedBuffer.Slice(written));
						written += SemiColon.Length;

						for (var i = 0; i < whitespaceCount; i++)
						{
							redactedBuffer[written++] = ' ';
						}
					}

					var key = cookieItem.Slice(0, equalsIndex);
					var value = cookieItem.Slice(equalsIndex + 1);

					key.CopyTo(redactedBuffer.Slice(written));
					written += key.Length;

					redactedBuffer.Slice(written++)[0] = '=';

					if (WildcardMatcher.IsAnyMatch(namesToSanitize, key.ToString()))
					{
						Redacted.CopyTo(redactedBuffer.Slice(written));
						written += Redacted.Length;
					}
					else
					{
						value.CopyTo(redactedBuffer.Slice(written));
						written += value.Length;
					}
				}
				else
				{
					cookieItem.CopyTo(redactedBuffer.Slice(written));
					written += cookieItem.Length;
				}

				writeSemiColon = true;
				cookieHeaderEntrySpan = cookieHeaderEntrySpan.Slice(foundSemiColon ? semiColonIndex + 1 : cookieItem.Length);

				whitespaceCount = 0;

				while (cookieHeaderEntrySpan.Length > 0)
				{
					if (cookieHeaderEntrySpan[0] != ' ')
						break;

					cookieHeaderEntrySpan = cookieHeaderEntrySpan.Slice(1);
					whitespaceCount++;
				}
			}

			cookieHeaderValueSpan = cookieHeaderValueSpan.Slice(foundComma ? commaIndex + 1 : commaIndex);
		}

		redactedCookieHeader = redactedBuffer.Slice(0, written).ToString();
		ArrayPool<char>.Shared.Return(redactedBufferArray);
		return redactedCookieHeader;
#endif
	}
}

