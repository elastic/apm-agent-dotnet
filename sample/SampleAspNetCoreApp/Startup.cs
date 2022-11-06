// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.NetCoreAll;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp.Data;
#if NET5_0
using OpenTelemetry;
using OpenTelemetry.Trace;
#endif

namespace SampleAspNetCoreApp
{
	public class Startup
	{
		public Startup(IConfiguration configuration) => Configuration = configuration;

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			ConfigureServicesExceptMvc(services);
			services.AddMvc();
		}

		public static void ConfigureServicesExceptMvc(IServiceCollection services)
		{
			const string connection = @"Data Source=blogging.db";
			services.AddDbContext<SampleDataContext>
				(options => options.UseSqlite(connection));

			services.AddDefaultIdentity<IdentityUser>()
				.AddEntityFrameworkStores<SampleDataContext>();

			services.Configure<IdentityOptions>(options =>
			{
				// Password settings
				// Not meant for production! To make testing/playing with the sample app we use very simple,
				// but insecure settings
				options.Password.RequireDigit = false;
				options.Password.RequireLowercase = false;
				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequireUppercase = false;
				options.Password.RequiredLength = 5;
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
#if NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0_OR_GREATER
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
#else
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
#endif
		{
			if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SKIP_AGENT_REGISTRATION")))
				app.UseAllElasticApm(Configuration);

			ConfigureAllExceptAgent(app);
		}

		public static void ConfigureAllExceptAgent(IApplicationBuilder app)
		{
			app.UseDeveloperExceptionPage();

			app.UseStaticFiles();
			app.UseCookiePolicy();

			ConfigureRoutingAndMvc(app);
		}

		public static void ConfigureRoutingAndMvc(IApplicationBuilder app)
		{
#if NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0_OR_GREATER
			app.UseRouting();

			app.UseAuthentication();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapAreaControllerRoute(
					"MyOtherArea",
					"MyOtherArea",
					"MyOtherArea/{controller=Home}/{action=Index}/{id?}");
				endpoints.MapControllerRoute(
					"MyArea",
					"{area:exists}/{controller=Home}/{action=Index}/{id?}");
				endpoints.MapControllerRoute(
					"default",
					"{controller=Home}/{action=Index}/{id?}");
				endpoints.MapControllers();
				endpoints.MapRazorPages();
			});
#else
			app.UseAuthentication();

			app.UseMvc(routes =>
			{
				routes.MapAreaRoute(
					"MyOtherArea",
					"MyOtherArea",
					"MyOtherArea/{controller=Home}/{action=Index}/{id?}");

				routes.MapRoute(
					"MyArea",
					"{area:exists}/{controller=Home}/{action=Index}/{id?}");

				routes.MapRoute(
					"default",
					"{controller=Home}/{action=Index}/{id?}");
			});
#endif
		}
	}
}
