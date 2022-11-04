// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.NetCoreAll;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
			services.AddMvc();


		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
#if NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0_OR_GREATER
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

#if NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0_OR_GREATER
			app.UseRouting();

			app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
#else
			app.UseMvc();
#endif
		}
	}
}
