// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
#if NET8_0_OR_GREATER
using System.Buffers;
#endif

namespace Elastic.Apm.Filters
{
	/// <summary>
	/// Redacts items from the cookie list of the Cookie request header.
	/// </summary>
	internal class CookieHeaderRedactionFilter
	{
		private const string CookieHeader = "Cookie";

		public static IError Filter(IError error)
		{
			if (error is Error e && e.Context is not null)
				HandleCookieHeader(e.Context?.Request?.Headers, e.Configuration.SanitizeFieldNames);
			return error;
		}

		public static ITransaction Filter(ITransaction transaction)
		{
			if (transaction is Transaction { IsContextCreated: true })
				HandleCookieHeader(transaction.Context?.Request?.Headers, transaction.Configuration.SanitizeFieldNames);
			return transaction;
		}

		// internal for testing
		internal static void HandleCookieHeader(Dictionary<string, string> headers, IReadOnlyList<WildcardMatcher> sanitizeFieldNames)
		{
			if (headers is not null)
			{
				// Realistically, this should be more than enough for all sensible scenarios
				// e.g. Cookies | cookies | COOKIES
				const int maxKeys = 4;

#if NET8_0_OR_GREATER
				var matchedKeys = ArrayPool<string>.Shared.Rent(maxKeys);
				var matchedValues = ArrayPool<string>.Shared.Rent(maxKeys);
#else
				var matchedKeys = new string[maxKeys];
				var matchedValues = new string[maxKeys];
#endif
				var matchedCount = 0;

				foreach (var header in headers)
				{
					if (matchedCount == maxKeys)
						break;

					if (header.Key.Equals(CookieHeader, StringComparison.OrdinalIgnoreCase))
					{
						matchedKeys[matchedCount] = header.Key;
						matchedValues[matchedCount] = CookieHeaderRedacter.Redact(header.Value, sanitizeFieldNames);
						matchedCount++;
					}
				}

				var replacedCount = 0;

				foreach (var headerKey in matchedKeys)
				{
					if (replacedCount == matchedCount)
						break;

					if (headerKey is not null)
					{
						headers[headerKey] = matchedValues[replacedCount];
						replacedCount++;
					}
				}

#if NET8_0_OR_GREATER
				ArrayPool<string>.Shared.Return(matchedKeys);
				ArrayPool<string>.Shared.Return(matchedValues);
#endif
			}
		}
	}
}
