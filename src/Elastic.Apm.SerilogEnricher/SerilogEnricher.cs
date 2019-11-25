using System;
using Serilog.Core;
using Serilog.Events;

namespace Elastic.Apm.SerilogEnricher
{
	public class SerilogEnricher : ILogEventEnricher
	{
		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			if (Agent.IsConfigured)
			{
				if (Agent.Tracer.CurrentSpan != null)
				{
					logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
						"span.id", Agent.Tracer.CurrentSpan.Id));
				}
				else if (Agent.Tracer.CurrentTransaction != null)
				{
					logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
						"transaction.id", Agent.Tracer.CurrentTransaction.Id));
					logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
						"trace.id", Agent.Tracer.CurrentTransaction.TraceId));
				}
			}
		}
	}
}
