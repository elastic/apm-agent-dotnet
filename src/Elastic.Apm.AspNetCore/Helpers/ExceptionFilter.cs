using System;
using Elastic.Apm.AspNetCore.Extensions;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
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
		internal static bool Capture(Exception e, Transaction transaction, HttpContext httpContext, IApmLogger logger)
		{
			transaction.CaptureException(e);

			transaction.CollectRequestBody(/* isForError: */ true, httpContext?.Request, logger);

			return false;
		}
	}
}
