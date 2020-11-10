// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore.Extensions;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Microsoft.AspNetCore.Http;

[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.PerfTests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Grpc.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]

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
			var createdTransaction = WebRequestTransactionCreator.StartTransactionAsync(context, _logger, _tracer, _agent.ConfigStore.CurrentSnapshot);

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
					transaction.CollectRequestBody(true, context.Request, _logger, transaction.ConfigSnapshot);

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
				transaction?.CollectRequestBody(true, context.Request, _logger, transaction.ConfigSnapshot);
			return false;
		}
	}
}
