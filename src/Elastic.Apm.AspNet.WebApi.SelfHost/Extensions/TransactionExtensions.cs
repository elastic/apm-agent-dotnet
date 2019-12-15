using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.AspNet.WebApi.SelfHost.Extensions
{
	internal static class TransactionExtensions
	{
		/// <summary>
		/// Collects the Request body in the transaction
		/// </summary>
		/// <param name="transaction">Transaction object</param>
		/// <param name="isForError">Is request body being captured for error (otherwise it's for transaction)</param>
		/// <param name="httpRequest">Current http request</param>
		/// <param name="logger">Logger object</param>
		internal static async Task CollectRequestBody(this Transaction transaction, bool isForError, HttpRequestMessage httpRequest, IApmLogger logger
		)
		{
			if (!transaction.IsSampled) return;

			if (httpRequest == null) return;

			string body = null;

			// Is request body already captured?
			// We check transaction.IsContextCreated to avoid creating empty Context (that accessing transaction.Context directly would have done).
			if (transaction.IsContextCreated
				&& transaction.Context.Request.Body != null
				&& !ReferenceEquals(transaction.Context.Request.Body, Consts.Redacted)) return;

			if (transaction.IsCaptureRequestBodyEnabled(isForError) && IsCaptureRequestBodyEnabledForContentType(transaction, httpRequest))
				body = await httpRequest.ExtractRequestBodyAsync(logger);

			// According to the documentation - the default value of 'body' is '[Redacted]'
			transaction.Context.Request.Body = body ?? Apm.Consts.Redacted;
		}

		internal static bool IsCaptureRequestBodyEnabled(this Transaction transaction, bool isForError) =>
			transaction.ConfigSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyAll)
			||
			(isForError
				? transaction.ConfigSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyErrors)
				: transaction.ConfigSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyTransactions));

		private static bool IsCaptureRequestBodyEnabledForContentType(Transaction transaction, HttpRequestMessage request)
		{
			//todo: it's temp
			if (request?.Content != null && request.Content.GetType() == typeof(StringContent)) return false;

			return true;

			// // We need to parse the content type and check it's not null and is of valid value
			// if (string.IsNullOrEmpty(request?.Content)) return false;
			//
			// var contentType = new ContentType(request.ContentType);
			//
			// //Request must not be null and the content type must be matched with the 'captureBodyContentTypes' configured
			// return transaction.ConfigSnapshot.CaptureBodyContentTypes.ContainsLike(contentType.MediaType);
		}
	}
}
