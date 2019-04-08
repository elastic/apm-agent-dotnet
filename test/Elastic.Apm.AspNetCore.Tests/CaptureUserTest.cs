using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{

	/// <summary>
	/// The ASP.NET Core auto instrumentation is able to capture logged in users.
	/// This class tests that feature.
	/// </summary>
	[Collection("DiagnosticListenerTest")]
	public class CaptureUserTest : IDisposable
	{
		private readonly MockPayloadSender _payloadSender = new MockPayloadSender();
		private ApmAgent _agent;

		private void SetUpSut()
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

		/// <summary>
		/// Integration test that makes sure that the ASP.NET Core auto instrumentation captures logged in users.
		/// It starts up the <see cref="SampleAspNetCoreApp"/> and creates a user
		/// then it logs in the user and then it sends an HTTP GET request to /Home/SimplePage (with the logged in user)
		/// It makes sure that this last transaction contains the user.
		/// This test uses the test app that is built on top of the default ASP.NET Core  <see cref="UserManager{TUser}"/>
		/// and <see cref="UserManager{TUser}"/>.
		/// </summary>
		[Fact]
		public async Task RegisterAndLogInUser()
		{
			SetUpSut();

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

		/// <summary>
		/// A unit test that directly calls InvokeAsync on the <see cref="ApmMiddleware"/>.
		/// It tests for OpenID claims.
		/// It creates a <see cref="DefaultHttpContext"/> with email and sub claims on it (those are OpenID standard)
		/// and make sure that the agent captured the userid and the email address of the user.
		/// </summary>
		[Fact]
		public async Task OpenIdClaimsTest()
		{
			const string mail = "my@mail.com";
			const string sub = "123-456";

			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				var context = new DefaultHttpContext
				{
					User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
					{
						new Claim("email", mail),
						new Claim("sub", sub),
					}, "someAuthTypeName"))
				};

				var middleware = new ApmMiddleware(async (innerHttpContext) => { await Task.Delay(1);}, agent.Tracer as Tracer, agent.ConfigurationReader);

				await middleware.InvokeAsync(context);
			}

			payloadSender.FirstTransaction.Context.User.Email.Should().Be(mail);
			payloadSender.FirstTransaction.Context.User.Id.Should().Be(sub);
		}

		public void Dispose() => _agent?.Dispose();
	}
}
