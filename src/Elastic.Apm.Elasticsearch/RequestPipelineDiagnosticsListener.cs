using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
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
			if (!TryGetCurrentElasticsearchSpan(out var span)) return;

			span.Name += $" ({response.HttpStatusCode})";

			RegisterStatement(span, response);
			RegisterError(span, response);

			Logger.Info()?.Log("Received an {Event} event from elasticsearch", @event);
			span.End();
		}

		private static void RegisterStatement(ISpan span, IApiCallDetails response)
		{
			//we can only register the statement if the client disables direct streaming
			if (response.RequestBodyInBytes == null) return;

			//make sure db exists
			var db = span.Context.Db ?? (span.Context.Db = new Database
			{
				Instance = response.Uri?.GetLeftPart(UriPartial.Authority), Type = Database.TypeElasticsearch,
			});

			db.Statement = Encoding.UTF8.GetString(response.RequestBodyInBytes);

		}

		private static void RegisterError(ISpan span, IApiCallDetails response)
		{
			if (response.Success) return;

			var exception = response.OriginalException ?? response.AuditTrail.FirstOrDefault(a => a.Exception != null)?.Exception;
			var f = PipelineFailure.Unexpected;
			// report inner exception stack traces for these directly if possible
			if (exception is ElasticsearchClientException es)
			{
				f = es.FailureReason ?? f;
				exception = es.InnerException ?? es;
			}
			if (exception is UnexpectedElasticsearchClientException un)
			{
				f = un.FailureReason ?? f;
				exception = un.InnerException ?? un;
			}

			var culprit = "Elasticsearch Error";

			var message = $"{f.GetStringValue()} {exception?.Message}";
			var stackFrames = exception == null ? new StackTrace(true).GetFrames() : new StackTrace(exception, true).GetFrames();

			var causeOnServer = false;
			if (response.ResponseBodyInBytes != null)
			{
				using (var memoryStream = new MemoryStream(response.ResponseBodyInBytes))
				{
					if (ServerError.TryCreate(memoryStream, out var serverError))
					{
						causeOnServer = true;
						culprit = "Elasticsearch Server Error";
						message = serverError.Error.RootCause.FirstOrDefault()?.Reason
							?? serverError.Error.CausedBy?.Reason
							?? serverError.Error.Reason;
					}
				}
			}

			if (exception == null && !causeOnServer) return;
			if (causeOnServer && string.IsNullOrEmpty(message)) return;

			span.CaptureError(message, culprit, stackFrames);
		}

		private void OnRequestData(string @event, RequestData requestData)
		{
			var name = ToName(@event);
			if (TryStartElasticsearchSpan(name, out var span, requestData?.Node?.Uri.ToString()))
			{
				if (@event == CallStart && requestData != null)
					span.Name = $"Elasticsearch: {requestData.Method.GetStringValue()} {requestData.Uri?.AbsolutePath}";

				Logger.Info()?.Log("Received an {Event} event from elasticsearch", @event);
			}
		}

		private const string PingStart = nameof(DiagnosticSources.RequestPipeline.Ping) + StartSuffix;
		private const string PingStop = nameof(DiagnosticSources.RequestPipeline.Ping) + StopSuffix;
		private const string SniffStart = nameof(DiagnosticSources.RequestPipeline.Sniff) + StartSuffix;
		private const string SniffStop = nameof(DiagnosticSources.RequestPipeline.Sniff) + StopSuffix;
		private const string CallStart = nameof(DiagnosticSources.RequestPipeline.CallElasticsearch) + StartSuffix;
		private const string CallStop = nameof(DiagnosticSources.RequestPipeline.CallElasticsearch) + StopSuffix;

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
					return @event.Replace(StartSuffix, string.Empty).Replace(StopSuffix, string.Empty);
			}
		}
	}
}
