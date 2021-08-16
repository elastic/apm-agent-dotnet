// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Elastic.Apm.Helpers
{
	internal static class Sanitization
	{
		/// <summary>
		/// Sanitizes a <see cref="HttpRequestMessage"/>, redacting user info from the request URI and HTTP headers that
		/// match any of the matchers.
		/// </summary>
		/// <remarks>
		/// The <see cref="HttpRequestMessage"/> instance is mutated, so this method should not be used for logging
		/// **before** using the instance in a request.
		/// </remarks>
		/// <param name="message">The message to sanitize</param>
		/// <param name="matchers">The matchers used to redact HTTP headers</param>
		/// <returns>A sanitized <see cref="HttpRequestMessage"/></returns>
		internal static HttpRequestMessage Sanitize(this HttpRequestMessage message, IReadOnlyList<WildcardMatcher> matchers)
		{
			if (message is null)
				return null;

			message.RequestUri = message.RequestUri.Sanitize();

			var headers = message.Headers.Select(h => h.Key).ToList();
			foreach (var header in headers)
			{
				if (WildcardMatcher.IsAnyMatch(matchers, header) && message.Headers.Remove(header))
					message.Headers.TryAddWithoutValidation(header, Consts.Redacted);
			}

			return message;
		}

		/// <summary>
		/// Redacts username and password, if present
		/// </summary>
		internal static Uri Sanitize(this Uri uri)
		{
			if (string.IsNullOrEmpty(uri.UserInfo))
				return uri;

			var builder = new UriBuilder(uri)
			{
				UserName = Consts.Redacted,
				Password = Consts.Redacted
			};
			return builder.Uri;
		}

		internal static bool Sanitize(Uri uri, out string result)
		{
			try
			{
				result = uri.Sanitize().ToString();
				return true;
			}
			catch
			{
				result = null;
				return false;
			}
		}

		internal static bool TrySanitizeUrl(string uri, out string sanitizedUri, out Uri originalUri)
		{
			try
			{
				originalUri = new Uri(uri, UriKind.RelativeOrAbsolute);
				return Sanitize(originalUri, out sanitizedUri);
			}
			catch
			{
				sanitizedUri = null;
				originalUri = null;
				return false;
			}
		}
	}
}
