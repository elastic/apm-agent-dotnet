// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
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

		internal AspNetCoreHttpRequest(HttpRequest request) => _request = request;

		public Task<ReadRequestBodyResult> ExtractBody(IConfiguration configuration, IApmLogger logger) => _request?.ExtractRequestBody(configuration);

		public bool HasValue => _request != null;
		public string ContentType => _request?.ContentType;

	}
}
