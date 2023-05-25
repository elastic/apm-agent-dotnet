// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elasticsearch.Net;
using Elasticsearch.Net.Diagnostics;

namespace Elastic.Apm.Elasticsearch
{
	public class RequestPipelineDiagnosticsListener : ElasticsearchDiagnosticsListenerBase
	{
		private const string CallStart = nameof(DiagnosticSources.RequestPipeline.CallElasticsearch) + StartSuffix;
		private const string CallStop = nameof(DiagnosticSources.RequestPipeline.CallElasticsearch) + StopSuffix;

		private const string PingStart = nameof(DiagnosticSources.RequestPipeline.Ping) + StartSuffix;
		private const string PingStop = nameof(DiagnosticSources.RequestPipeline.Ping) + StopSuffix;
		private const string SniffStart = nameof(DiagnosticSources.RequestPipeline.Sniff) + StartSuffix;
		private const string SniffStop = nameof(DiagnosticSources.RequestPipeline.Sniff) + StopSuffix;

		public RequestPipelineDiagnosticsListener(IApmAgent agent) : base(agent) =>
			Observer = new RequestPipelineDiagnosticObserver(
				a => OnRequestData(a.Key, a.Value),
				a => OnResult(a.Key, a.Value)
			);

		public override string Name => DiagnosticSources.RequestPipeline.SourceName;

		private void OnResult(string @event, IApiCallDetails response)
		{
			if (!TryGetCurrentElasticsearchSpan(out var span)) return;

			if (response != null)
			{
				span.Name += $" ({response.HttpStatusCode})";

				RegisterStatement(span, response);
				RegisterError(span, response);

				if (response.Success)
					span.Outcome = Outcome.Success;
				else
					span.Outcome = Outcome.Failure;
			}
			else
			{
				span.Name += " (Exception)";
				span.Outcome = Outcome.Failure;
			}


			Logger.Info()?.Log("Received an {Event} event from elasticsearch", @event);
			span.End();
		}

		private static void RegisterStatement(ISpan span, IApiCallDetails response)
		{
			//make sure db exists
			var db = span.Context.Db ?? (span.Context.Db = new Database
			{
				Instance = response.Uri?.GetLeftPart(UriPartial.Authority), Type = Database.TypeElasticsearch
			});

			db.Statement = response.DebugInformation;
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

			var culprit = "Client Error";

			var message = $"{f.GetStringValue()} {exception?.Message}";
			var stackFrames = exception == null ? null : new StackTrace(exception, true).GetFrames();
			if (stackFrames == null || stackFrames.Length == 0)
				stackFrames = new StackTrace(true).GetFrames();

			var causeOnServer = false;
			if (response.ResponseBodyInBytes != null)
			{
				using var memoryStream = new MemoryStream(response.ResponseBodyInBytes);
				if (ServerError.TryCreate(memoryStream, out var serverError) && serverError != null)
				{
					causeOnServer = true;
					culprit = $"Elasticsearch Server Error: {serverError.Error.Type}";
					message = $"The server returned a ({response.HttpStatusCode}) and indicated: " + (
						serverError.Error?.CausedBy?.Reason
						?? serverError.Error?.CausedBy?.Type
						?? serverError.Error?.RootCause.FirstOrDefault()?.Reason
						?? serverError.Error?.Reason
						?? "Response did not indicate a server error, usually means no json was with an error key was returned.");
				}
			}

			if (exception == null && !causeOnServer) return;
			if (causeOnServer && string.IsNullOrEmpty(message)) return;

			if (causeOnServer)
				span.CaptureError(message, culprit, stackFrames);
			else span.CaptureException(exception);
		}

		private void OnRequestData(string @event, RequestData requestData)
		{
			var name = ToName(@event);
			if (TryStartElasticsearchSpan(name, out var span, requestData?.Node?.Uri))
			{
				if (@event == CallStart && requestData != null)
					span.Name = $"Elasticsearch: {requestData.Method.GetStringValue()} {requestData.Uri?.AbsolutePath}";

				Logger.Info()?.Log("Received an {Event} event from elasticsearch", @event);
			}
		}

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
					return @event?.Replace(StartSuffix, string.Empty).Replace(StopSuffix, string.Empty);
			}
		}
	}
}
