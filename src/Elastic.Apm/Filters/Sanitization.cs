// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Filters
{
	internal static class Sanitization
	{
		public static void SanitizeHeadersInContext(Context context, IConfiguration configuration)
		{
			if (context?.Request?.Headers is not null)
				RedactMatches(context?.Request?.Headers, configuration);

			if (context?.Request?.Cookies is not null)
				RedactMatches(context?.Request?.Cookies, configuration);

			if (context?.Response?.Headers is not null)
				RedactMatches(context?.Response?.Headers, configuration);

			if (context?.Message?.Headers is not null)
				RedactMatches(context?.Message?.Headers, configuration);

			static void RedactMatches(Dictionary<string, string> dictionary, IConfiguration configuration)
			{
				foreach (var key in dictionary.Keys.ToArray())
				{
					if (WildcardMatcher.IsAnyMatch(configuration.SanitizeFieldNames, key))
						dictionary[key] = Consts.Redacted;
				}
			}
		}
	}
}
