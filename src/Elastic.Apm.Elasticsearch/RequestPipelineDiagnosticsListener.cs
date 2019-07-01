using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elasticsearch.Net;
using Elasticsearch.Net.Diagnostics;

namespace Elastic.Apm.Elasticsearch
{
	public class RequestPipelineDiagnosticsListener : ElasticsearchDiagnosticsListenerBase
	{
		public RequestPipelineDiagnosticsListener(IApmAgent agent) : base(agent, DiagnosticSources.RequestPipeline.SourceName) =>
			Observer = new RequestPipelineDiagnosticObserver(
				a => OnRequestData(a.Key, a.Value),
				a => OnResult(a.Key, a.Value)
			);

		private void OnResult(string @event, IApiCallDetails response)
		{
			if (!@event.EndsWith(".Stop")) return;

			var id = Activity.Current.Id;
			if (!Spans.TryRemove(id, out var span)) return;

			span.Context.Db = new Database
			{
				Instance = response.Uri.ToString(),
				Type = Database.TypeElasticsearch
			};
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

			var id = Activity.Current.Id;
			if (Spans.TryAdd(id, span)) return;

			Logger.Error()?.Log("Failed to add to ProcessingRequests - ???");
		}

		private const string PingStart = nameof(DiagnosticSources.RequestPipeline.Ping) + ".Start";
		private const string PingStop = nameof(DiagnosticSources.RequestPipeline.Ping) + ".Stop";
		private const string SniffStart = nameof(DiagnosticSources.RequestPipeline.Sniff) + ".Start";
		private const string SniffStop = nameof(DiagnosticSources.RequestPipeline.Sniff) + ".Stop";
		private const string CallStart = nameof(DiagnosticSources.RequestPipeline.CallElasticsearch) + ".Start";
		private const string CallStop = nameof(DiagnosticSources.RequestPipeline.CallElasticsearch) + ".Stop";

		private static string ToName(string @event)
		{
			switch (@event)
			{
				case PingStart:
				case PingStop:
					return DiagnosticSources.RequestPipeline.Ping;
				case SniffStart:
				case SniffStop:
					return DiagnosticSources.RequestPipeline.Sniff;
				case CallStart:
				case CallStop:
					return DiagnosticSources.RequestPipeline.CallElasticsearch;
				default:
					return @event.Replace(".Start", string.Empty).Replace(".Stop", string.Empty);

			}
		}


	}
}
