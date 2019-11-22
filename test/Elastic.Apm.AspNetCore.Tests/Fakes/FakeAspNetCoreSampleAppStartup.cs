using Elastic.Apm.AspNetCore.Tests.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp;

namespace Elastic.Apm.AspNetCore.Tests.Fakes
{
	public class FakeAspNetCoreSampleAppStartup : Startup
	{
		public FakeAspNetCoreSampleAppStartup(IConfiguration configuration) : base(configuration) { }

		public override void ConfigureAgent(IApplicationBuilder app)
		{
			var startupConfigService = app.ApplicationServices.GetService<StartupConfigService>();

			if (startupConfigService.UseElasticApm)
			{
				app.UseElasticApm(startupConfigService.Agent, startupConfigService.Subscribers);
			}
			else
			{
				app.UseMiddleware<ApmMiddleware>(startupConfigService.Agent.Tracer, startupConfigService.Agent);
			}

			if (startupConfigService.UseDeveloperExceptionPage)
			{
				app.UseDeveloperExceptionPage();
			}
		}
	}
}
