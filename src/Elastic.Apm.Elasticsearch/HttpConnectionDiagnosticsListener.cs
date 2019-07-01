using System;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elasticsearch.Net;
using Elasticsearch.Net.Diagnostics;

namespace Elastic.Apm.Elasticsearch
{
	public class HttpConnectionDiagnosticsListener : ElasticsearchDiagnosticsListenerBase
	{
		public HttpConnectionDiagnosticsListener(IApmAgent agent) : base(agent, DiagnosticSources.HttpConnection.SourceName) =>
			Observer = new HttpConnectionDiagnosticObserver(
				a => OnRequestData(a.Key, a.Value),
				a => OnResult(a.Key, a.Value)
			);

		private void OnResult(string @event, int? statusCode)
		{
			if (!@event.EndsWith(".Stop")) return;

			var id = Activity.Current.Id;
			if (!Spans.TryRemove(id, out var span)) return;

			span.Action = span.Name;
			span.End();
		}


		private void OnRequestData(string @event, RequestData requestData)
		{
			var transaction = Agent.TransactionContainer.Transactions.Value;
			if (Agent.TransactionContainer.Transactions == null || Agent.TransactionContainer.Transactions.Value == null)
			{
				Logger.Debug()?.Log("No active transaction, skip creating span for outgoing HTTP request");
				return;
			}
			var name = ToName(@event);
			if (!@event.EndsWith(".Start")) return;

			var span = transaction.StartSpanInternal(name,
				//TODO types
				ApiConstants.TypeDb,
				ApiConstants.SubtypeHttp);
			span.Context.Db = new Database
			{
				Instance = requestData.Node?.Uri.ToString(),
				Type = Database.TypeElasticsearch
			};

			var id = Activity.Current.Id;
			if (Spans.TryAdd(id, span)) return;

			Logger.Error()?.Log("Failed to add to ProcessingRequests - ???");
		}

		private const string ReceiveStart = nameof(DiagnosticSources.HttpConnection.ReceiveBody) + ".Start";
		private const string ReceiveStop = nameof(DiagnosticSources.HttpConnection.ReceiveBody) + ".Stop";
		private const string SendStart = nameof(DiagnosticSources.HttpConnection.SendAndReceiveHeaders) + ".Start";
		private const string SendStop = nameof(DiagnosticSources.HttpConnection.SendAndReceiveHeaders) + ".Stop";

		private static string ToName(string @event)
		{
			switch (@event)
			{
				case ReceiveStart:
				case ReceiveStop:
					return DiagnosticSources.HttpConnection.ReceiveBody;
				case SendStart:
				case SendStop:
					return DiagnosticSources.HttpConnection.SendAndReceiveHeaders;
				default:
					return @event.Replace(".Start", string.Empty).Replace(".Stop", string.Empty);

			}
		}


	}
}
