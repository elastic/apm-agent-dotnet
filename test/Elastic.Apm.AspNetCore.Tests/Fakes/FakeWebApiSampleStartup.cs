using Elastic.Apm.AspNetCore.Tests.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApiSample;

namespace Elastic.Apm.AspNetCore.Tests.Fakes
{
	public class FakeWebApiSampleStartup : Startup
	{
		public FakeWebApiSampleStartup(IConfiguration configuration) : base(configuration) { }

		public override void ConfigureAgent(IApplicationBuilder app)
		{
			var agentService = app.ApplicationServices.GetService<StartupConfigService>();
			app.UseElasticApm(agentService.Agent, agentService.Subscribers);
		}
	}
}
