// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Web;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.AspNetFullFramework.Extensions;
using Elastic.Apm.AspNetCore;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNetFullFramework
{
	internal class AspNetHttpRequest : IHttpRequestAdapter
	{
		private readonly HttpRequest _request;

		public AspNetHttpRequest(HttpRequest request) => _request = request;

		public bool HasValue => _request != null;
		public string ContentType => _request?.ContentType;
		public string ExtractBody(IConfiguration configuration, IApmLogger logger, out bool longerThanMaxLength)
		{
			string body = null;
			longerThanMaxLength = false;

			if (_request != null)
			{
				if (_request.HasFormContentType())
				{
					var form = new AspNetHttpForm(_request.Form);
					body = form.AsSanitizedString(configuration, out longerThanMaxLength);
				}
				else
				{
					if (_request.ReadEntityBodyMode == ReadEntityBodyMode.Bufferless)
					{
						logger.Warning()?.Log($"Request body capturing is not possible 'bufferless' mode");
					}
					else
					{
						body = RequestBodyStreamHelper.ToString(_request.InputStream, out longerThanMaxLength);
					}
				}
			}

			return body;
		}
	}
}
