using Elastic.Apm.All;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiSample
{
	public class Startup
	{
		public Startup(IConfiguration configuration) => Configuration = configuration;

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services) => services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			ConfigureAgent(app);

			app.UseHttpsRedirection();
			app.UseMvc();
		}

		public virtual void ConfigureAgent(IApplicationBuilder app)
		{
			app.UseElasticApm(Configuration);
			app.UseDeveloperExceptionPage();
		}
	}
}
