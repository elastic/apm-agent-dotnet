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
			var transaction = Agent.TransactionContainer.Transactions.Value;
			if (Agent.TransactionContainer.Transactions == null || Agent.TransactionContainer.Transactions.Value == null)
			{
				Logger.Debug()?.Log("No active transaction, skip creating span for outgoing HTTP request");
				return;
			}
			var name = ToName(@event);

			var id = Activity.Current.Id;
			if (@event.EndsWith(".Start"))
			{
				var span = transaction.StartSpanInternal(name,
					//TODO types
					ApiConstants.TypeDb,
					ApiConstants.SubtypeHttp);

				if (Spans.TryAdd(id, span)) return;

				Logger.Error()?.Log("Failed to add to ProcessingRequests - ???");
			}
			else if (@event.EndsWith(".Stop"))
			{
				if (!Spans.TryRemove(id, out var span)) return;

				span.Action = name;
				span.Tags.Add(nameof(serializerInfo.Purpose), serializerInfo.Purpose);
				span.End();
			}
		}

		private const string SerializeStart = nameof(DiagnosticSources.Serializer.Serialize) + ".Start";
		private const string SerializeStop = nameof(DiagnosticSources.Serializer.Serialize) + ".Stop";
		private const string DeserializeStart = nameof(DiagnosticSources.Serializer.Deserialize) + ".Start";
		private const string DeserializeStop = nameof(DiagnosticSources.Serializer.Deserialize) + ".Stop";

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
					return @event.Replace(".Start", string.Empty).Replace(".Stop", string.Empty);

			}
		}


	}
}
