// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;
using System.IO;
using System.Text;
using System;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	internal interface IHttpRequestAdapter
	{
		bool HasValue { get; }
		string ContentType { get; }
		string ExtractBody(IConfiguration configuration, out bool longerThanMaxLength);
	}

	internal static class IHttpRequestAdapterExtensions
	{
		/// <summary>
		/// Extracts the request body, up to a specified maximum length.
		/// The request body that is read is buffered.
		/// </summary>
		/// <param name="httpRequest">The request object (ASP.NET or ASP.NET Core)</param>
		/// <param name="logger">The logger</param>
		/// <param name="configuration">The configuration snapshot</param>
		/// <returns></returns>
		internal static string ExtractBody(this IHttpRequestAdapter httpRequest, IApmLogger logger, IConfiguration configuration)
		{
			var longerThanMaxLength = false;
			try
			{
				return httpRequest.ExtractBody(configuration, out longerThanMaxLength);
			}
			catch (IOException ioException)
			{
				logger.Error()?.LogException(ioException, "IO Error reading request body");
			}
			catch (Exception e)
			{
				logger.Error()?.LogException(e, "Error reading request body");
			}

			if (longerThanMaxLength)
				logger.Debug()?.Log("truncated body to max length {MaxLength}", RequestBodyStreamHelper.RequestBodyMaxLength);

			return null;
		}
	}
}
