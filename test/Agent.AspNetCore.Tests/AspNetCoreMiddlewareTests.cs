using System;
using System.Threading.Tasks;
using Agent.AspNetCore.Tests.Mock;
using Elastic.Agent.AspNetCore;
using Elastic.Agent.Core.DiagnosticSource;
using Elastic.Agent.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;


using SampleAspNetCoreApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Agent.AspNetCore.Tests
{
    public class AspNetCoreMiddlewareTests
        : IClassFixture<WebApplicationFactory<SampleAspNetCoreApp.Startup>>
    {
        private readonly WebApplicationFactory<SampleAspNetCoreApp.Startup> factory;

        public AspNetCoreMiddlewareTests(WebApplicationFactory<SampleAspNetCoreApp.Startup> factory)
         => this.factory = factory;

        /// <summary>
        /// Simulates and HTTP GET call to /home/about and asserts on what the agent should send to the server
        /// </summary>
        [Theory]
        [InlineData("/Home/About")]
        public async Task HomeAboutTransactionTest(string url)
        {           
            var client = factory
                .WithWebHostBuilder(n =>
            {
                n.Configure(app =>
                {
                    app.UseElasticApm(new MockPayloadSender());
                    new ElasticCoreListeners().Start();
                    new ElasticEntityFrameworkCoreListener().Start();

                    app.UseDeveloperExceptionPage();

                    app.UseHsts();

                    app.UseHttpsRedirection();
                    app.UseStaticFiles();
                    app.UseCookiePolicy();

                    app.UseMvc(routes =>
                    {
                        routes.MapRoute(
                            name: "default",
                            template: "{controller=Home}/{action=Index}/{id?}");
                    });
                });

                n.ConfigureServices((services) =>
                {
                    services.Configure<CookiePolicyOptions>(options =>
                    {
                        options.CheckConsentNeeded = context => true;
                        options.MinimumSameSitePolicy = SameSiteMode.None;
                    });

                    var connection = @"Data Source=blogging.db";
                    services.AddDbContext<SampleDataContext>
                        (options => options.UseSqlite(connection));

                    services.AddMvc()
                            //this is needed bacuase of a (probably) bug:
                            //https://github.com/aspnet/Mvc/issues/5992
                            .AddApplicationPart(Assembly.Load(new AssemblyName("SampleAspNetCoreApp")))
                            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
                });
            })
            .CreateClient();

            var response = await client.GetAsync(url);

            // Assert
            //WIP
        }
    }
}
