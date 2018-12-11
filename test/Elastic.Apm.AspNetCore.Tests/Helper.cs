using System;
using System.Net.Http;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using SampleAspNetCoreApp.Data;
using Microsoft.EntityFrameworkCore;
using Elastic.Apm.Tests.Mock;

namespace Elastic.Apm.AspNetCore.Tests
{
    public static class Helper
    {
        internal static HttpClient GetClient<T>(MockPayloadSender payloadSender, WebApplicationFactory<T> factory)  where T : class
        => factory
            .WithWebHostBuilder(n =>
            {
                n.Configure(app =>
                {
                    app.UseElasticApm(payloadSender: payloadSender);
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
                            .AddApplicationPart(Assembly.Load(new AssemblyName(nameof(SampleAspNetCoreApp))))
                            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
                });
            })
        .CreateClient();
    }
}
