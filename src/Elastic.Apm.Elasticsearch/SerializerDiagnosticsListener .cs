using System;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elasticsearch.Net;
using Elasticsearch.Net.Diagnostics;

namespace Elastic.Apm.Elasticsearch
{
	public class SerializerDiagnosticsListener : ElasticsearchDiagnosticsListenerBase
	{
		public SerializerDiagnosticsListener(IApmAgent agent) : base(agent, DiagnosticSources.Serializer.SourceName) =>
			Observer = new SerializerDiagnosticObserver(a => OnSerializer(a.Key, a.Value));

		private void OnSerializer(string @event, SerializerRegistrationInformation serializerInfo)
		{
			// the high level .NET client NEST also emits events when (de)serializing user defined types e.g:
			// 		- _source
			//		- script parameters
			//		- script values
			// This is to granular of information for tracing and is more there to aid profiling.
			if (serializerInfo.Purpose != "request/response") return;

			var name = ToName(@event);

			if (@event.EndsWith(StartSuffix) && TryStartElasticsearchSpan(name, out _))
				Logger.Info()?.Log("Received an {Event} event from elasticsearch", @event);
			else if (@event.EndsWith(StopSuffix) && TryGetCurrentElasticsearchSpan(out var span))
			{
				Logger.Info()?.Log("Received an {Event} event from elasticsearch", @event);
				span.End();
			}
		}

		private const string SerializeStart = nameof(DiagnosticSources.Serializer.Serialize) + StartSuffix;
		private const string SerializeStop = nameof(DiagnosticSources.Serializer.Serialize) + StopSuffix;
		private const string DeserializeStart = nameof(DiagnosticSources.Serializer.Deserialize) + StartSuffix;
		private const string DeserializeStop = nameof(DiagnosticSources.Serializer.Deserialize) + StopSuffix;

		private static string ToName(string @event)
		{
			switch (@event)
			{
				case SerializeStart:
				case SerializeStop:
					return DiagnosticSources.Serializer.Serialize;
				case DeserializeStart:
				case DeserializeStop:
					return DiagnosticSources.Serializer.Deserialize;
				default:
					return @event.Replace(StartSuffix, string.Empty).Replace(StopSuffix, string.Empty);

			}
		}


	}
}
