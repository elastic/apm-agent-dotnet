// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Net.Mime;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.Extensions
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
		internal static void CollectRequestBody(this ITransaction transaction, bool isForError, IHttpRequestAdapter httpRequest, IApmLogger logger)
		{
			if (!transaction.IsSampled)
				return;

			if (httpRequest == null || !httpRequest.HasValue)
				return;

			string body = null;

			// Is request body already captured?
			// We check transaction.IsContextCreated to avoid creating empty Context (that accessing transaction.Context directly would have done).
			var hasContext = (transaction is Transaction t && t.IsContextCreated) || transaction.Context != null;
			if (hasContext
				&& transaction.Context.Request.Body != null
				&& !ReferenceEquals(transaction.Context.Request.Body, Consts.Redacted))
				return;

			if (transaction.IsCaptureRequestBodyEnabled(isForError) && IsCaptureRequestBodyEnabledForContentType(transaction, httpRequest?.ContentType, logger))
				body = httpRequest.ExtractBody(logger, transaction.Configuration);

			// According to the documentation - the default value of 'body' is '[Redacted]'
			transaction.Context.Request.Body = body ?? Consts.Redacted;
		}

		internal static bool IsCaptureRequestBodyEnabled(this ITransaction transaction, bool isForError) =>
			transaction.Configuration.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyAll)
			||
			(isForError
				? transaction.Configuration.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyErrors)
				: transaction.Configuration.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyTransactions));

		private static bool IsCaptureRequestBodyEnabledForContentType(ITransaction transaction, string requestContentType, IApmLogger logger)
		{
			// We need to parse the content type and check it's not null and is of valid value
			if (string.IsNullOrEmpty(requestContentType))
				return false;

			try
			{
				var contentType = new ContentType(requestContentType);

				// Request must not be null and the content type must be matched with the 'captureBodyContentTypes' configured
				return transaction.Configuration.CaptureBodyContentTypes.ContainsLike(contentType.MediaType);
			}
			catch (Exception ex)
			{
				logger?.Info()?.LogException(ex, "Exception occurred when capturing '{ContentType}' content type", requestContentType);
				return false;
			}
		}

		internal static void SetOutcomeForHttpResult(this ITransaction transaction, int httpReturnCode)
		{
			if (transaction is Transaction realTransaction)
			{
				if (httpReturnCode >= 500 || httpReturnCode < 100)
					realTransaction.SetOutcome(Outcome.Failure);
				else
					realTransaction.SetOutcome(Outcome.Success);
			}
		}
	}
}
