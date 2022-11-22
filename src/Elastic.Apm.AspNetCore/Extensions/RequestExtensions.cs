// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore.Extensions
{
	internal static class HttpRequestExtensions
	{
		internal static string ExtractRequestBody(this HttpRequest request, IConfiguration configuration,
			out bool longerThanMaxLength)
		{
			var result = AsyncHelper.RunSync(() => ExtractRequestBodyAsync(request, configuration));
			longerThanMaxLength = result.IsLongerThanMaxLength;
			return result.Body;
		}

		private static async Task<ReadRequestBodyResult> ExtractRequestBodyAsync(this HttpRequest request,
			IConfiguration configuration)
		{
			// Ensure the request Microsoft.AspNetCore.Http.HttpRequest.Body can be read multiple times
			request.EnableBuffering();

			if (request.HasFormContentType)
			{
				var formCollection = await request.ReadFormAsync();
				var form = new AspNetCoreHttpForm(formCollection);
				var body = form.AsSanitizedString(configuration, out var longerThanMaxLength);
				return new ReadRequestBodyResult(body, longerThanMaxLength);
			}

			return await RequestBodyStreamHelper.ToString(request.Body);
		}
	}
}
