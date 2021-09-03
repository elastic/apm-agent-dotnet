// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
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
		/// <param name="httpRequest">Current http request</param>
		/// <param name="logger">Logger object</param>
		internal static void CollectRequestBody(this Transaction transaction, bool isForError, HttpRequest httpRequest, IApmLogger logger)
		{
			if (!transaction.IsSampled) return;

			if (httpRequest == null) return;

			string body = null;

			// Is request body already captured?
			// We check transaction.IsContextCreated to avoid creating empty Context (that accessing transaction.Context directly would have done).
			if (transaction.IsContextCreated
				&& transaction.Context.Request.Body != null
				&& !ReferenceEquals(transaction.Context.Request.Body, Apm.Consts.Redacted)) return;

			if (transaction.IsCaptureRequestBodyEnabled(isForError) && IsCaptureRequestBodyEnabledForContentType(transaction, httpRequest, logger))
				body = httpRequest.ExtractRequestBody(logger, transaction.ConfigurationSnapshot);

			// According to the documentation - the default value of 'body' is '[Redacted]'
			transaction.Context.Request.Body = body ?? Apm.Consts.Redacted;
		}

		internal static bool IsCaptureRequestBodyEnabled(this Transaction transaction, bool isForError) =>
			transaction.ConfigurationSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyAll)
			||
			(isForError
				? transaction.ConfigurationSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyErrors)
				: transaction.ConfigurationSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyTransactions));

		private static bool IsCaptureRequestBodyEnabledForContentType(Transaction transaction, HttpRequest request, IApmLogger logger)
		{
			// We need to parse the content type and check it's not null and is of valid value
			if (string.IsNullOrEmpty(request?.ContentType)) return false;

			try
			{
				var contentType = new ContentType(request.ContentType);

				//Request must not be null and the content type must be matched with the 'captureBodyContentTypes' configured
				return transaction.ConfigurationSnapshot.CaptureBodyContentTypes.ContainsLike(contentType.MediaType);
			}
			catch (Exception ex)
			{
				logger?.Info()?.LogException(ex, "Exception occurred when capturing '{ContentType}' content type", request.ContentType);
				return false;
			}
		}
	}
}
