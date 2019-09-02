using System.Net.Mime;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore.Extensions
{
	public static class TransactionExtensions
	{
		/// <summary>
		/// Collects the Request body (and possibly additional information in the future)
		/// in the transaction
		/// </summary>
		/// <param name="transaction">Transaction object</param>
		/// <param name="httpContext">Current http context</param>
		/// <param name="logger">Logger object</param>
		public static void CollectRequestInfo(this ITransaction transaction, HttpContext httpContext, IConfigurationReader configurationReader, IApmLogger logger)
		{
			var body = Consts.BodyRedacted; // According to the documentation - the default value of 'body' is '[Redacted]'

			// We need to parse the content type and check it's not null and is of valid value 
			if (!string.IsNullOrEmpty(httpContext?.Request?.ContentType))
			{
				var contentType = new ContentType(httpContext.Request.ContentType);

				//Request must not be null and the content type must be matched with the 'captureBodyContentTypes' configured
				if (httpContext?.Request != null && configurationReader.CaptureBodyContentTypes.ContainsLike(contentType.MediaType))
				{
					body = httpContext.Request.ExtractRequestBody(logger);
				}
				transaction.Context.Request.Body = string.IsNullOrEmpty(body) ? Consts.BodyRedacted : body;
			}
		}
	}
}
