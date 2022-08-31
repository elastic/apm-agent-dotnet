// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System;
using System.Web;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.AspNetFullFramework.Extensions;

namespace Elastic.Apm.AspNetFullFramework
{
	internal class AspNetHttpRequest : IHttpRequestAdapter
	{
		private readonly HttpRequest _request;

		public AspNetHttpRequest(HttpRequest request) => _request = request;

		public bool HasValue => _request != null;
		public string ContentType => _request?.ContentType;
		public string ExtractBody(IConfiguration configuration, out bool longerThanMaxLength)
		{
			string body = null;
			longerThanMaxLength = false;

			if (_request != null)
			{
				if (_request.HasFormContentType())
				{
					var form = _request.Form;
					if (form != null)
					{
						var itemProcessed = 0;
						var sb = new StringBuilder();
						foreach (var key in _request.Form.AllKeys)
						{
							sb.Append(key);
							sb.Append("=");

							if (WildcardMatcher.IsAnyMatch(configuration.SanitizeFieldNames, key))
								sb.Append(Consts.Redacted);
							else
								sb.Append(_request.Form[key]);

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
					body = RequestBodyStreamHelper.ToString(_request.InputStream, out longerThanMaxLength);

			}

			return body;
		}
	}
}
