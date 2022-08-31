// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Text;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Elastic.Apm.AspNetCore.Extensions
{
	internal static class HttpRequestExtensions
	{
		internal static string ExtractRequestBody(this HttpRequest request, IConfiguration configuration, out bool longerThanMaxLength)
		{
			string body = null;
			longerThanMaxLength = false;

			//Ensure the request Microsoft.AspNetCore.Http.HttpRequest.Body can be read multiple times
			request.EnableBuffering();

			if (request.HasFormContentType)
			{
				var form = request.Form;

				var itemProcessed = 0;
				if (form != null && form.Count > 0)
				{
					var sb = new StringBuilder();
					foreach (var item in form)
					{
						sb.Append(item.Key);
						sb.Append("=");

						if (WildcardMatcher.IsAnyMatch(configuration.SanitizeFieldNames, item.Key))
							sb.Append(Apm.Consts.Redacted);
						else
							sb.Append(item.Value);

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
			}
			else
			{
				// allow synchronous reading of the request stream, which is false by default from 3.0 onwards.
				// Reading must be synchronous as it can happen within a synchronous diagnostic listener method
				var bodyControlFeature = request.HttpContext.Features.Get<IHttpBodyControlFeature>();
				if (bodyControlFeature != null)
					bodyControlFeature.AllowSynchronousIO = true;

				body = RequestBodyStreamHelper.ToString(request.Body, out longerThanMaxLength);
			}

			return body;
		}
	}
}
