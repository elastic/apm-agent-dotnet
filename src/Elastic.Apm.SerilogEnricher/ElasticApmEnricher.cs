using Serilog.Core;
using Serilog.Events;

namespace Elastic.Apm.SerilogEnricher
{
	public class ElasticApmEnricher : ILogEventEnricher
	{
		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			if (!Agent.IsConfigured) return;
			if (Agent.Tracer.CurrentTransaction == null) return;

			logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
				"TransactionId", Agent.Tracer.CurrentTransaction.Id));
			logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
				"TraceId", Agent.Tracer.CurrentTransaction.TraceId));
		}
	}
}
