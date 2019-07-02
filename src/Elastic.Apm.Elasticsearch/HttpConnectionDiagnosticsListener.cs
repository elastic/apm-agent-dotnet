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
			if (!TryGetCurrentElasticsearchSpan(out var span)) return;

			span.Name += $" ({statusCode})";
			Logger.Info()?.Log("Received an {Event} event from elasticsearch", @event);
			span.End();
		}

		private void OnRequestData(string @event, RequestData requestData)
		{
			var name = ToName(@event);
			if (TryStartElasticsearchSpan(name, out _, requestData?.Node?.Uri.ToString()))
				Logger.Info()?.Log("Received an {Event} event from elasticsearch", @event);
		}

		private const string ReceiveStart = nameof(DiagnosticSources.HttpConnection.ReceiveBody) + StartSuffix;
		private const string ReceiveStop = nameof(DiagnosticSources.HttpConnection.ReceiveBody) + StopSuffix;
		private const string SendStart = nameof(DiagnosticSources.HttpConnection.SendAndReceiveHeaders) + StartSuffix;
		private const string SendStop = nameof(DiagnosticSources.HttpConnection.SendAndReceiveHeaders) + StopSuffix;

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
