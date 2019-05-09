using AspNetCoreSampleApp.Data;
using Elastic.Apm.All;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSampleApp
{
	public class Startup
	{
		public Startup(IConfiguration configuration) => Configuration = configuration;

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddDbContext<SampleDataContext>
				(options => options.UseSqlite(@"Data Source=blogging.db"));

			services.AddDefaultIdentity<IdentityUser>()
				.AddEntityFrameworkStores<SampleDataContext>();

			services.Configure<IdentityOptions>(options =>
			{
				// Password settings
				// Not meant for production! To make testing/playing with the sample app we use very simple, but insecure settings.
				options.Password.RequireDigit = false;
				options.Password.RequireLowercase = false;
				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequireUppercase = false;
				options.Password.RequiredLength = 5;
			});

			services.AddMvc()
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			ConfigureAgent(app);

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseCookiePolicy();
			app.UseAuthentication();

			app.UseMvc(routes =>
			{
				routes.MapRoute("default","{controller=Home}/{action=Index}/{id?}");
			});
		}

		public virtual void ConfigureAgent(IApplicationBuilder app)
		{
			app.UseElasticApm(Configuration);
			app.UseDeveloperExceptionPage();
		}
	}
}
