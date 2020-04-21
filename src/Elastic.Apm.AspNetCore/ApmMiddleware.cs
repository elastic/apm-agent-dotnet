// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore.Extensions;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
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

namespace Elastic.Apm.AspNetCore
{
	// ReSharper disable once ClassNeverInstantiated.Global
	internal class ApmMiddleware
	{
		private readonly IApmLogger _logger;

		private readonly RequestDelegate _next;
		private readonly Tracer _tracer;
		private readonly IConfigurationReader _configurationReader;

		public ApmMiddleware(RequestDelegate next, Tracer tracer, IApmAgent agent)
		{
			_next = next;
			_tracer = tracer;
			_logger = agent.Logger.Scoped(nameof(ApmMiddleware));
			_configurationReader = agent.ConfigurationReader;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			var transaction = WebRequestTransactionCreator.StartTransactionAsync(context, _logger, _tracer);
			if (transaction != null)
				await WebRequestTransactionCreator.FillSampledTransactionContextRequest(transaction, context, _logger);

			try
			{
				await _next(context);
			}
			catch (Exception e) when (transaction != null)
			{
				transaction.CaptureException(e);
				// It'd be nice to have this in an exception filter, but that would force us capturing the request body synchronously.
				// Therefore we rather unwind the stack in the catch block and call the async method.
				if (context != null)
					await transaction.CollectRequestBodyAsync(true, context.Request, _logger, transaction.ConfigSnapshot);

				throw;
			}
			finally
			{
				WebRequestTransactionCreator.StopTransaction(transaction, context, _logger);
			}
		}
	}
}
