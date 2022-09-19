// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System;
using Elastic.Apm.Config;
using System.Collections.Generic;

namespace Elastic.Apm.Helpers
{
	internal interface IHttpFormAdapter : IEnumerable<(string Key, string Value)>
	{
		bool HasValue { get; }
		int Count { get; } 
	}

	internal static class IHttpFormAdapterExtensions
	{
		internal static string AsSanitizedString(this IHttpFormAdapter form, IConfiguration configuration, out bool longerThanMaxLength)
		{
			longerThanMaxLength = false;
			string body = null;

			var itemProcessed = 0;
			if (form != null && form.HasValue && form.Count > 0)
			{
				var sb = new StringBuilder();
				foreach (var (key, value) in form)
				{
					sb.Append(key);
					sb.Append("=");

					if (WildcardMatcher.IsAnyMatch(configuration.SanitizeFieldNames, key))
						sb.Append(Consts.Redacted);
					else
						sb.Append(value);

					itemProcessed++;
					if (itemProcessed != form.Count)
						sb.Append("&");

					// perf: check length once per iteration and truncate at the end, rather than each append
					if (sb.Length > RequestBodyStreamHelper.RequestBodyMaxLength)
					{
						longerThanMaxLength = true;
						break;
					}
				}

				body = sb.ToString(0, Math.Min(sb.Length, RequestBodyStreamHelper.RequestBodyMaxLength));
			}
			return body;
		}
	}
}
