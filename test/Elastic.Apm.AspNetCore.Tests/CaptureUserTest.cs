using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Integration test that makes sure that the ASP.NET Core auto instrumentation captures logged in users.
	/// It starts up the <see cref="SampleAspNetCoreApp"/> and creates a user
	/// then it logs in the user and then it sends an HTTP GET request to /Home/SimplePage (with the logged in user)
	/// It makes sure that this last transaction contains the user.
	/// </summary>
	[Collection("DiagnosticListenerTest")]
	public class CaptureUserTest : IDisposable
	{
		private readonly MockPayloadSender _payloadSender = new MockPayloadSender();
		private ApmAgent _agent;

		public CaptureUserTest()
		{
			var unused = Program.CreateWebHostBuilder(null)
				.Configure(app =>
				{
					_agent = new ApmAgent(new AgentComponents(payloadSender: _payloadSender));
					app.UseElasticApm(_agent);
					Startup.ConfigureAllExceptAgent(app);
				})
				.ConfigureServices(services =>
				{
					Startup.ConfigureServicesExceptMvc(services);
					services.AddMvc()
						//this is needed because of a (probably) bug:
						//https://github.com/aspnet/Mvc/issues/5992
						.AddApplicationPart(Assembly.Load(new AssemblyName(nameof(SampleAspNetCoreApp))))
						.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
				})
				.UseUrls("http://localhost:5900") //CI doesn't like https, so we roll with http
				.Build()
				.RunAsync();
		}

		[Fact]
		public async Task RegisterAndLogInUse()
		{
			const string userName = "TestUser";
			const string password = "aaaaaa";

			var client = new HttpClient() { BaseAddress = new Uri("http://localhost:5900") };

			//Home/Index runs the migrations, so this makes sure the DB exists
			var homeResult = await client.GetAsync("/Home/Index");
			homeResult.IsSuccessStatusCode.Should().BeTrue();

			//create user
			var formContent = new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("username", userName),
				new KeyValuePair<string, string>("password", password)
			});

			await client.PostAsync("/Account/RegisterUser", formContent);

			//login:
			var loginResult = await client.PostAsync("/Account/LoginUser", formContent);
			loginResult.IsSuccessStatusCode.Should().BeTrue();

			//get /home/simplepage with the user
			var simplePage = await client.GetAsync("/Home/SimplePage");
			simplePage.IsSuccessStatusCode.Should().BeTrue();

			var transactionWithUser = _payloadSender.Transactions.OrderByDescending(n => (n as Transaction)?.Timestamp).First();

			transactionWithUser
				.Context.User.UserName.Should()
				.Be(userName);
		}

		public void Dispose() => _agent?.Dispose();
	}
}
