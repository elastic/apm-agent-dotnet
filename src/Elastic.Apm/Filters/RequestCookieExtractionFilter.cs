// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;

namespace Elastic.Apm.Filters
{
	/// <summary>
	/// Extracts cookies from the Cookie request header and sets the Cookie header to [REDACTED].
	/// </summary>
	internal class RequestCookieExtractionFilter
	{
		private static readonly WildcardMatcher[] CookieMatcher = new WildcardMatcher[] { new WildcardMatcher.VerbatimMatcher("Cookie", true) };

		public static IError Filter(IError error)
		{
			if (error is Error realError)
				HandleCookieHeader(realError.Context);
			return error;
		}

		public static ITransaction Filter(ITransaction transaction)
		{
			if (transaction is Transaction { IsContextCreated: true })
				HandleCookieHeader(transaction.Context);
			return transaction;
		}

		private static void HandleCookieHeader(Context context)
		{
			if (context?.Request?.Headers is not null)
			{
				string matchedKey = null;
				foreach (var key in context.Request.Headers.Keys)
				{
					if (WildcardMatcher.IsAnyMatch(CookieMatcher, key))
					{
						var cookies = context.Request.Headers[key];
						context.Request.Cookies = CookieHeaderParser.ParseCookies(cookies);
						matchedKey = key;
					}
				}

				if (matchedKey is not null)
					context.Request.Headers[matchedKey] = Consts.Redacted;
			}
		}
	}
}
