// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elasticsearch.Net;
using Elasticsearch.Net.Diagnostics;

namespace Elastic.Apm.Elasticsearch
{
	public class HttpConnectionDiagnosticsListener : ElasticsearchDiagnosticsListenerBase
	{
		public HttpConnectionDiagnosticsListener(IApmAgent agent) : base(agent) =>
			Observer = new HttpConnectionDiagnosticObserver(
				a => OnRequestData(a.Key, a.Value),
				a => OnResult(a.Key, a.Value)
			);

		public override string Name => DiagnosticSources.HttpConnection.SourceName;

		private void OnResult(string @event, int? statusCode)
		{
			if (!TryGetCurrentElasticsearchSpan(out var span)) return;

			span.Name += $" ({statusCode})";
			Logger.Info()?.Log("Received an {Event} event from elasticsearch", @event);
			if (statusCode.HasValue)
				span.Context.Http.StatusCode = statusCode.Value;
			span.End();
		}

		private void OnRequestData(string @event, RequestData requestData)
		{
			var name = ToName(@event);
			if (requestData == null) return;

			var instanceUri = requestData.Node?.Uri;
			if (TryStartElasticsearchSpan(name, out var span, instanceUri))
			{
				Logger.Info()?.Log("Received an {Event} event from elasticsearch", @event);
				var requestUri = requestData.Uri;
				span.Context.Http = new Http
				{
					Method = requestData.Method.GetStringValue(),
					Url = requestUri?.PathAndQuery
				};
			}
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
					return @event?.Replace(".Start", string.Empty).Replace(".Stop", string.Empty);

			}
		}
	}
}
