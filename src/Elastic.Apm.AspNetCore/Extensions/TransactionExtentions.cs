using System.Net.Mime;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore.Extensions
{
	internal static class TransactionExtensions
	{
		/// <summary>
		/// Collects the Request body in the transaction
		/// </summary>
		/// <param name="transaction">Transaction object</param>
		/// <param name="isForError">Is request body being captured for error (otherwise it's for transaction)</param>
		/// <param name="request">Current http request</param>
		/// <param name="logger">Logger object</param>
		internal static void CollectRequestBody(this Transaction transaction, bool isForError, HttpRequest request, IApmLogger logger)
		{
			if (!transaction.IsSampled) return;

			// Is request body already captured?
			// We check transaction.IsContextCreated to avoid creating empty Context (that accessing transaction.Context directly would have done).
			if (transaction.IsContextCreated
				&& transaction.Context.Request.Body != null
				&& !ReferenceEquals(transaction.Context.Request.Body, Apm.Consts.Redacted)) return;

			string body = null;
			if (IsCaptureRequestBodyEnabled(transaction, isForError) && IsCaptureRequestBodyEnabledForContentType(transaction, request))
				body = request.ExtractRequestBody(logger);
			// According to the documentation - the default value of 'body' is '[Redacted]'
			transaction.Context.Request.Body = body ?? Apm.Consts.Redacted;
		}

		private static bool IsCaptureRequestBodyEnabled(Transaction transaction, bool isForError) =>
			transaction.ConfigSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyAll)
			||
			(isForError
				? transaction.ConfigSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyErrors)
				: transaction.ConfigSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyTransactions));

		private static bool IsCaptureRequestBodyEnabledForContentType(Transaction transaction, HttpRequest request)
		{
			// We need to parse the content type and check it's not null and is of valid value
			if (string.IsNullOrEmpty(request?.ContentType)) return false;

			var contentType = new ContentType(request.ContentType);

			//Request must not be null and the content type must be matched with the 'captureBodyContentTypes' configured
			return transaction.ConfigSnapshot.CaptureBodyContentTypes.ContainsLike(contentType.MediaType);
		}
	}
}
