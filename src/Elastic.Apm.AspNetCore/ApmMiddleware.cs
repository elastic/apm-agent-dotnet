// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore.Extensions;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore
{
	// ReSharper disable once ClassNeverInstantiated.Global
	internal class ApmMiddleware
	{
		private readonly IApmLogger _logger;

		private readonly RequestDelegate _next;
		private readonly Tracer _tracer;
		private readonly ApmAgent _agent;

		public ApmMiddleware(RequestDelegate next, Tracer tracer, ApmAgent agent)
		{
			_next = next;
			_tracer = tracer;
			_logger = agent.Logger.Scoped(nameof(ApmMiddleware));
			_agent = agent;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			var createdTransaction = WebRequestTransactionCreator.StartTransactionAsync(context, _logger, _tracer, _agent.ConfigurationStore.CurrentSnapshot);

			Transaction transaction = null;
			if (createdTransaction is Transaction t)
				transaction = t;

			if (transaction != null)
				WebRequestTransactionCreator.FillSampledTransactionContextRequest(transaction, context, _logger);

			try
			{
				await _next(context);
			}
			catch (Exception e) when (CaptureExceptionAndRequestBody(e, context, transaction)) { }
			finally
			{
				// In case an error handler middleware is registered, the catch block above won't be executed, because the
				// error handler handles all the exceptions - in this case, based on the response code and the config, we may capture the body here
				if (transaction != null && transaction.IsContextCreated && context?.Response.StatusCode >= 400
					&& transaction.Context?.Request?.Body is string body
					&& (string.IsNullOrEmpty(body) || body == Apm.Consts.Redacted))
					transaction.CollectRequestBody(true, context.Request, _logger);

				if(transaction != null)
					WebRequestTransactionCreator.StopTransaction(transaction, context, _logger);
				else
					createdTransaction?.End();
			}
		}

		private bool CaptureExceptionAndRequestBody(Exception e, HttpContext context, Transaction transaction)
		{
			transaction?.CaptureException(e);
			if (context != null)
				transaction?.CollectRequestBody(true, context.Request, _logger);
			return false;
		}
	}
}
