using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Model;

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
			var currentActiviry = Activity.Current;
			if (kv.Key == "Grpc.Net.Client.GrpcOut.Start")
			{
				var requestObject = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Request")?.GetValue(kv.Value) as System.Net.Http.HttpRequestMessage;



				var currentTransaction = _agent.Tracer.CurrentTransaction;

				if (currentTransaction != null)
				{
					var grpcMethodName = currentActiviry.Tags.Where(n => n.Key == "grpc.method").FirstOrDefault().Value;
					var newSpan = currentTransaction.StartSpan(grpcMethodName, ApiConstants.TypeExternal, "grpc");
					ProcessingRequests.TryAdd(requestObject, newSpan);

				}
			}
			if (kv.Key == "Grpc.Net.Client.GrpcOut.Stop")
			{
				var requestObject = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Request")?.GetValue(kv.Value) as System.Net.Http.HttpRequestMessage;
				var currentTransaction = _agent.Tracer.CurrentTransaction;

				var grpcStatusCode = currentActiviry.Tags.Where(n => n.Key == "grpc.status_code").FirstOrDefault().Value;

				if(ProcessingRequests.TryGetValue(requestObject, out var span))
				{
					span.End();
				}
			}
		}

		private static string GrpcReturnCodeToString(string returnCode)
		{
			if (int.TryParse(returnCode, out var intValue))
			{
				switch (intValue)
				{
					case 0:
						return "OK";
					case 1:
						return "CANCELLED";
					case 2:
						return "UNKNOWN";
					case 3:
						return "INVALID_ARGUMENT";
					case 4:
						return "DEADLINE_EXCEEDED";
					case 5:
						return "NOT_FOUND";
					case 6:
						return "ALREADY_EXISTS";
					case 7:
						return "PERMISSION_DENIED";
					case 8:
						return "RESOURCE_EXHAUSTED";
					case 9:
						return "FAILED_PRECONDITION";
					case 10:
						return "ABORTED";
					case 11:
						return "OUT_OF_RANGE";
					case 12:
						return "UNIMPLEMENTED";
					case 13:
						return "INTERNAL";
					case 14:
						return "UNAVAILABLE";
					case 15:
						return "DATA_LOSS";
					case 16:
						return "UNAUTHENTICATED";
					default:
						return "UNDEFINED";
				}
			}
			return "UNDEFINED";
		}
	}
}
