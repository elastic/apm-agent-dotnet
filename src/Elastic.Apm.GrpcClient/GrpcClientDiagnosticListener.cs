using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.GrpcClient
{
	public class GrpcClientDiagnosticListener : IDiagnosticListener
	{
		private readonly IApmAgent _agent;

		internal readonly ConcurrentDictionary<System.Net.Http.HttpRequestMessage, ISpan> ProcessingRequests = new ConcurrentDictionary<System.Net.Http.HttpRequestMessage, ISpan>();

		public GrpcClientDiagnosticListener(IApmAgent apmAgent) => _agent = apmAgent;

		public string Name => "Grpc.Net.Client";

		public void OnCompleted() { }
		public void OnError(Exception error) { }
		public void OnNext(KeyValuePair<string, object> kv)
		{
			_agent?.Logger.Trace()?.Log("{currentClassName} received diagnosticsource event: {eventKey}", nameof(GrpcClientDiagnosticListener), kv.Key);

			var currentActiviry = Activity.Current;
			if (kv.Key == "Grpc.Net.Client.GrpcOut.Start")
			{
				var requestObject = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Request")?.GetValue(kv.Value) as System.Net.Http.HttpRequestMessage;

				var currentTransaction = _agent.Tracer.CurrentTransaction;

				if (currentTransaction != null)
				{
					var grpcMethodName = currentActiviry?.Tags.Where(n => n.Key == "grpc.method")?.FirstOrDefault().Value;

					if (string.IsNullOrEmpty(grpcMethodName))
						grpcMethodName = "unknown";

					_agent?.Logger.Trace()?.Log("Starting span for gRPC call, method:{methodName}", grpcMethodName);
					var newSpan = currentTransaction.StartSpan(grpcMethodName, ApiConstants.TypeExternal, ApiConstants.SubTypeGrpc);
					ProcessingRequests.TryAdd(requestObject, newSpan);
				}
			}
			if (kv.Key == "Grpc.Net.Client.GrpcOut.Stop")
			{
				var requestObject = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Request")?.GetValue(kv.Value) as System.Net.Http.HttpRequestMessage;
				var currentTransaction = _agent.Tracer.CurrentTransaction;

				if (ProcessingRequests.TryGetValue(requestObject, out var span))
				{
					_agent?.Logger.Trace()?.Log("Ending span for gRPC call, span:{span}", span);

					var grpcStatusCode = currentActiviry?.Tags.Where(n => n.Key == "grpc.status_code")?.FirstOrDefault().Value;

					if (GrpcHelper.GrpcReturnCodeToString(grpcStatusCode) == "OK")
						span.Outcome = Outcome.Success;
					else
						span.Outcome = Outcome.Failure;

					span.Context.Destination = UrlUtils.ExtractDestination(requestObject.RequestUri, _agent.Logger);
					span.Context.Destination.Service = UrlUtils.ExtractService(requestObject.RequestUri, span);

					span.End();
				}
			}
		}
	}
}
