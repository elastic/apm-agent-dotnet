using System;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elasticsearch.Net;
using Elasticsearch.Net.Diagnostics;

namespace Elastic.Apm.Elasticsearch
{
	public class AuditDiagnosticsListener : ElasticsearchDiagnosticsListenerBase
	{
		public AuditDiagnosticsListener(IApmAgent agent) : base(agent, DiagnosticSources.AuditTrailEvents.SourceName) =>
			Observer = new AuditDiagnosticObserver(a => OnAudit(a.Key, a.Value));

		private void OnAudit(string @event, Audit audit)
		{
			var transaction = Agent.TransactionContainer.Transactions.Value;
			if (Agent.TransactionContainer.Transactions == null || Agent.TransactionContainer.Transactions.Value == null)
			{
				Logger.Debug()?.Log("No active transaction, skip creating span for outgoing HTTP request");
				return;
			}
			var name = @audit.Event.GetStringValue();

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

				span.Context.Db = new Database
				{
					Instance = audit.Node?.Uri.ToString(),
					Type = Database.TypeElasticsearch
				};
				span.Action = name;
				span.End();
			}
		}

	}
}
