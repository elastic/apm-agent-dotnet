﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.GrpcClient
{
	public class GrpcClientDiagnosticListener : IDiagnosticListener
	{
		private readonly IApmAgent _agent;
		internal readonly ConcurrentDictionary<HttpRequestMessage, ISpan> ProcessingRequests = new ConcurrentDictionary<HttpRequestMessage, ISpan>();

		public GrpcClientDiagnosticListener(IApmAgent apmAgent) => _agent = apmAgent;

		public string Name => "Grpc.Net.Client";

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(KeyValuePair<string, object> kv)
		{
			_agent?.Logger.Trace()
				?.Log("{currentClassName} received diagnosticsource event: {eventKey}", nameof(GrpcClientDiagnosticListener), kv.Key);

			var currentActivity = Activity.Current;
			if (kv.Key == "Grpc.Net.Client.GrpcOut.Start")
			{
				var requestObject = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Request")?.GetValue(kv.Value) as HttpRequestMessage;
				if (requestObject != null)
				{
					var currentTransaction = _agent?.Tracer.CurrentTransaction;

					if (currentTransaction != null)
					{
						var grpcMethodName = currentActivity?.Tags?.FirstOrDefault(n => n.Key == "grpc.method").Value;

						if (string.IsNullOrEmpty(grpcMethodName))
							grpcMethodName = "unknown";

						_agent?.Logger.Trace()?.Log("Starting span for gRPC call, method:{methodName}", grpcMethodName);
						var newSpan = currentTransaction.StartSpan(grpcMethodName, ApiConstants.TypeExternal, ApiConstants.SubTypeGrpc);
						ProcessingRequests.TryAdd(requestObject, newSpan);
					}
				}
			}
			if (kv.Key == "Grpc.Net.Client.GrpcOut.Stop")
			{
				var requestObject = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Request")?.GetValue(kv.Value) as HttpRequestMessage;

				if (requestObject == null) return;

				if (!ProcessingRequests.TryRemove(requestObject, out var span)) return;

				_agent?.Logger.Trace()?.Log("Ending span for gRPC call, span:{span}", span);

				var grpcStatusCode = currentActivity?.Tags?.Where(n => n.Key == "grpc.status_code").FirstOrDefault().Value;
				if (grpcStatusCode != null)
					span.Outcome = GrpcHelper.GrpcReturnCodeToString(grpcStatusCode) == "OK" ? Outcome.Success : Outcome.Failure;

				span.Context.Destination = UrlUtils.ExtractDestination(requestObject.RequestUri, _agent?.Logger);
				span.Context.Destination.Service = UrlUtils.ExtractService(requestObject.RequestUri, span);

				span.End();
			}
		}
	}
}
