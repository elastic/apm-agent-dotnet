// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.GrpcClient
{
	public class GrpcClientDiagnosticListener : DiagnosticListenerBase
	{
		internal readonly ConcurrentDictionary<HttpRequestMessage, ISpan> ProcessingRequests = new ConcurrentDictionary<HttpRequestMessage, ISpan>();

		public GrpcClientDiagnosticListener(IApmAgent apmAgent) : base(apmAgent) { }

		public override string Name => "Grpc.Net.Client";

		protected override void HandleOnNext(KeyValuePair<string, object> kv)
		{
			Logger.Trace()
				?.Log("{currentClassName} received diagnosticsource event: {eventKey}", nameof(GrpcClientDiagnosticListener), kv.Key);

			var currentActivity = Activity.Current;
			if (kv.Key == "Grpc.Net.Client.GrpcOut.Start")
			{
				if (kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Request")?.GetValue(kv.Value) is HttpRequestMessage requestObject)
				{
					var currentTransaction = ApmAgent?.Tracer.CurrentTransaction;

					if (currentTransaction != null)
					{
						var grpcMethodName = currentActivity?.Tags.FirstOrDefault(n => n.Key == "grpc.method").Value;

						if (string.IsNullOrEmpty(grpcMethodName))
							grpcMethodName = "unknown";

						Logger.Trace()?.Log("Starting span for gRPC call, method:{methodName}", grpcMethodName);
						var newSpan = currentTransaction.StartSpan(grpcMethodName, ApiConstants.TypeExternal, ApiConstants.SubTypeGrpc);
						ProcessingRequests.TryAdd(requestObject, newSpan);
					}
				}
			}
			if (kv.Key == "Grpc.Net.Client.GrpcOut.Stop")
			{
				if (kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Request")?.GetValue(kv.Value) is not HttpRequestMessage requestObject)
					return;

				if (!ProcessingRequests.TryRemove(requestObject, out var span))
					return;

				Logger.Trace()?.Log("Ending span for gRPC call, span:{span}", span);

				var grpcStatusCode = currentActivity?.Tags?.Where(n => n.Key == "grpc.status_code").FirstOrDefault().Value;
				if (grpcStatusCode != null)
					span.Outcome = GrpcHelper.GrpcClientReturnCodeToOutcome(GrpcHelper.GrpcReturnCodeToString(grpcStatusCode));

				span.Context.Destination = UrlUtils.ExtractDestination(requestObject.RequestUri, Logger);
				span.Context.Destination.Service = UrlUtils.ExtractService(requestObject.RequestUri, span);

				span.End();
			}
		}
	}
}
