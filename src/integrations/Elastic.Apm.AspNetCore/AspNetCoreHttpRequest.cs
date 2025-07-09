// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.AspNetCore.Extensions;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore
{
	internal class AspNetCoreHttpRequest : IHttpRequestAdapter
	{
		private readonly HttpRequest _request;

		internal AspNetCoreHttpRequest(HttpRequest request, IConfiguration configuration)
		{
			_request = request;
			if (configuration != null && configuration.CaptureBody == ConfigConsts.SupportedValues.CaptureBodyErrors)
			{
				_request?.EnableBuffering();
			}
		}

		internal AspNetCoreHttpRequest(HttpRequest request) => _request = request;

		public string ExtractBody(IConfiguration configuration, IApmLogger logger, out bool longerThanMaxLength)
		{
			longerThanMaxLength = false;

			var shouldBeBuffered = configuration?.CaptureBody == ConfigConsts.SupportedValues.CaptureBodyErrors;

			// Enable buffering if CaptureBody is set to "errors"
			if (shouldBeBuffered)
			{
				_request?.EnableBuffering();
				// Reset stream position to the beginning in case the body was already read
				_request.Body.Position = 0;
			}

			var bodyContent = _request?.ExtractRequestBody(configuration, out longerThanMaxLength);

			// Reset stream position if buffering was enabled
			if (shouldBeBuffered)
			{
				_request.Body.Position = 0;
			}

			return bodyContent;
		}

		public bool HasValue => _request != null;
		public string ContentType => _request?.ContentType;

	}
}
