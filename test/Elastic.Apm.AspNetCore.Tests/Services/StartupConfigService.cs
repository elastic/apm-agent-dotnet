using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.AspNetCore.Tests.Services
{
	internal class StartupConfigService
	{
		public StartupConfigService(ApmAgent agent, bool useElasticApm = true, bool useDeveloperExceptionPage = true, params IDiagnosticsSubscriber[] subscribers)
		{
			Agent = agent;
			Subscribers = subscribers;
			UseElasticApm = useElasticApm;
			UseDeveloperExceptionPage = useDeveloperExceptionPage;
		}

		public ApmAgent Agent { get; }

		public IDiagnosticsSubscriber[] Subscribers { get; }

		public bool UseElasticApm { get; }

		public bool UseDeveloperExceptionPage { get; }
	}
}
