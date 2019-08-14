using System;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore.Extensions;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore.Helpers
{
	/// <summary>
	/// Offers methods that can be used as exception filters.
	/// Within the filter we return false and capture the exception.
	/// Advantage: it avoid stack unwinding.
	/// </summary>
	internal static class ExceptionFilter
	{
		internal static bool Capture(Exception e, ITransaction transaction, HttpContext httpContext, IConfigurationReader configurationReader, IApmLogger logger)
		{
			transaction.CaptureException(e);

			if (httpContext != null && configurationReader.ShouldExtractRequestBodyOnError())
				transaction.CollectRequestInfo(httpContext, configurationReader, logger);
			
			return false;
		}
	}
}
