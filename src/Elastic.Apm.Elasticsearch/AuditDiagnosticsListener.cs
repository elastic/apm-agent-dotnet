// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Logging;
using Elasticsearch.Net;
using Elasticsearch.Net.Diagnostics;

namespace Elastic.Apm.Elasticsearch
{
	public class AuditDiagnosticsListener : ElasticsearchDiagnosticsListenerBase
	{
		public AuditDiagnosticsListener(IApmAgent agent) : base(agent) =>
			Observer = new AuditDiagnosticObserver(a => OnAudit(a.Key, a.Value));

		public override string Name => DiagnosticSources.AuditTrailEvents.SourceName;

		private void OnAudit(string @event, Audit audit)
		{
			var name = @audit.Event.GetStringValue();

			if (@event.EndsWith(StartSuffix) && TryStartElasticsearchSpan(name, out _, audit.Node?.Uri))
				Logger.Info()?.Log("Received an {Event} event from elasticsearch", @event);
			else if (@event.EndsWith(StopSuffix) && TryGetCurrentElasticsearchSpan(out var span, audit.Node?.Uri))
			{
				Logger.Info()?.Log("Received an {Event} event from elasticsearch", @event);
				span.End();
			}
		}
	}
}
