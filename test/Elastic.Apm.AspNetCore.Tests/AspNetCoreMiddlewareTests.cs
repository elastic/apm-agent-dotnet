using System;
using System.Threading.Tasks;
using Elastic.Apm.AspNetCore.Tests.Mock;
using Elastic.Apm.AspNetCore;
using Elastic.Apm.EntityFrameworkCore;
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
using System.Net.Http;
using Elastic.Apm;
using Elastic.Apm.DiagnosticSource;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Elastic.Apm.AspNetCore.Tests
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
            var capturedPayload = new MockPayloadSender();
            var client = GetClient(capturedPayload);

            var response = await client.GetAsync(url);

            Assert.Single(capturedPayload.Payloads);
            Assert.Single(capturedPayload.Payloads[0].Transactions);

            var payload = capturedPayload.Payloads[0];

            //test payload
            Assert.Equal(Assembly.GetEntryAssembly()?.GetName()?.Name, payload.Service.Name);
            Assert.Equal(Consts.AgentName, payload.Service.Agent.Name);
            Assert.Equal(Consts.AgentVersion, payload.Service.Agent.Version);

            var transaction = capturedPayload.Payloads[0].Transactions[0];

            //test transaction
            Assert.Equal($"{response.RequestMessage.Method.ToString()} {response.RequestMessage.RequestUri.AbsolutePath}", transaction.Name);
            Assert.Equal("HTTP 2xx", transaction.Result);
            Assert.True(transaction.Duration > 0);
            Assert.Equal("request", transaction.Type);

            //test transaction.context.response
            Assert.Equal(200, transaction.Context.Response.Status_code);

            //test transaction.context.request
            Assert.Equal("2.0", transaction.Context.Request.HttpVersion);
            Assert.Equal("GET", transaction.Context.Request.Method);
            
            //test transaction.context.request.url
            Assert.Equal(response.RequestMessage.RequestUri.AbsolutePath, transaction.Context.Request.Url.Full);
            Assert.Equal("localhost", transaction.Context.Request.Url.HostName);
            Assert.Equal("HTTP", transaction.Context.Request.Url.Protocol);

            //test transaction.context.request.encrypted
            Assert.False(transaction.Context.Request.Socket.Encrypted);
        }

        private HttpClient GetClient(MockPayloadSender payloadSender)
            => factory
                .WithWebHostBuilder(n =>
                {
                    n.Configure(app =>
                    {
                        app.UseElasticApm(payloadSender);
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
