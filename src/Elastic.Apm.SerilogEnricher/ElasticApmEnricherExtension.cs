using System;
using Serilog;
using Serilog.Configuration;

namespace Elastic.Apm.SerilogEnricher
{

	public static class ElasticApmEnricherExtension
	{
		/// <summary>
		/// Enrich log events with a trace and transaction id properties containing the
		/// current ids that the Elastic APM .NET Agent generated.
		/// </summary>
		/// <param name="enrichmentConfiguration">Logger enrichment configuration.</param>
		/// <returns>Configuration object allowing method chaining.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="enrichmentConfiguration"/> is null.</exception>
		public static LoggerConfiguration WithElasticApmTraceId(
			this LoggerEnrichmentConfiguration enrichmentConfiguration
		)
		{
			if (enrichmentConfiguration == null) throw new ArgumentNullException(nameof(enrichmentConfiguration));

			return enrichmentConfiguration.With<ElasticApmEnricher>();
		}
	}
}
