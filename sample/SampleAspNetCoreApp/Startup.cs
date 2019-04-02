using System;
using Elastic.Apm.All;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp.Data;

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

			services.AddMvc()
				.AddMvcOptions(options =>
				{
					options.Filters.Add(new IgnoreAntiforgeryTokenAttribute());
					//options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute());
				})
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
		}

		public static void ConfigureServicesExceptMvc(IServiceCollection services)
		{
			services.Configure<CookiePolicyOptions>(options =>
			{
				// This lambda determines whether user consent for non-essential cookies is needed for a given request.
				options.CheckConsentNeeded = context => false;
				options.MinimumSameSitePolicy = SameSiteMode.None;
			});

			var connection = @"Data Source=blogging.db";
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
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			app.UseElasticApm(Configuration);

			ConfigureAllExceptAgent(app);
		}

		public static void ConfigureAllExceptAgent(IApplicationBuilder app)
		{
			app.UseDeveloperExceptionPage();

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseCookiePolicy();

			app.UseAuthentication();
			app.UseMvc(routes =>
			{
				routes.MapRoute(
					"default",
					"{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}
