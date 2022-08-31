// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Text;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Elastic.Apm.AspNetCore.Extensions
{
	internal static class HttpRequestExtensions
	{
		internal static string ExtractRequestBody(this HttpRequest request, IConfiguration configuration, out bool longerThanMaxLength)
		{
			//Ensure the request Microsoft.AspNetCore.Http.HttpRequest.Body can be read multiple times
			request.EnableBuffering();

			if (request.HasFormContentType)
			{
				var form = new AspNetCoreHttpForm(request.Form);
				return form.AsSanitizedString(configuration, out longerThanMaxLength);
			}
			else
			{
				// allow synchronous reading of the request stream, which is false by default from 3.0 onwards.
				// Reading must be synchronous as it can happen within a synchronous diagnostic listener method
				var bodyControlFeature = request.HttpContext.Features.Get<IHttpBodyControlFeature>();
				if (bodyControlFeature != null)
					bodyControlFeature.AllowSynchronousIO = true;

				return RequestBodyStreamHelper.ToString(request.Body, out longerThanMaxLength);
			}
		}
	}
}
