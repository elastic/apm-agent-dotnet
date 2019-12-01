using Elastic.Apm.NetCoreAll;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiSample
{
	public class Startup
	{
		private readonly IConfiguration _configuration;

		public Startup(IConfiguration configuration) => _configuration = configuration;

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services) =>
#if NETCOREAPP3_0
			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
#elif NETCOREAPP2_2
			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
#else
			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
#endif


		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
#if NETCOREAPP3_0
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
#else
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
#endif
		{
			app.UseAllElasticApm(_configuration);
			ConfigureAllExceptAgent(app);
		}

		public static void ConfigureAllExceptAgent(IApplicationBuilder app)
		{
			app.UseDeveloperExceptionPage();

			app.UseHttpsRedirection();
#if NETCOREAPP3_0
			app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
#else
			app.UseMvc();
#endif
		}
	}
}
