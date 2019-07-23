using System;
using System.IO;
using System.Text;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Microsoft.AspNetCore.Http;
using Elastic.Apm.Api;

namespace Elastic.Apm.AspNetCore.Extensions
{
    public static class TransactionExtensions
    {
		/// <summary>
		/// Collects the Request body (and possibily additional information in the future)
		/// in the transaction
		/// </summary>
		/// <param name="transaction">Transaction object</param>
		/// <param name="httpContext">Current http context</param>
		/// <param name="logger">Logger object</param>
		public static void CollectRequestInfo(this ITransaction transaction, HttpContext httpContext, IApmLogger logger)
		{
			var body = Consts.BodyRedacted; // According to the documentation - the default value of 'body' is '[Redacted]'
			if (httpContext?.Request != null)
			{
				body = httpContext.Request.ExtractRequestBody(logger);
			}
			transaction.Context.Request.Body = string.IsNullOrEmpty(body) ? Consts.BodyRedacted : body;
		}


	}
}
