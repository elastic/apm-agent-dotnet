// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

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
		private readonly ITestOutputHelper _output;

		public CaptureUserTest(ITestOutputHelper output) => _output = output;

		private void SetUpSut()
		{
			var unused = Program.CreateWebHostBuilder(null)
				.Configure(app =>
				{
					_agent = new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender, configuration: new MockConfiguration(exitSpanMinDuration: "0")));
					app.UseElasticApm(_agent, _agent.Logger);
					Startup.ConfigureAllExceptAgent(app);
				})
				.ConfigureLogging(logging => logging.AddXunit(_output))
				.ConfigureServices(services =>
				{
					Startup.ConfigureServicesExceptMvc(services);
					services
						.AddMvc()
						//this is needed because of a (probably) bug:
						//https://github.com/aspnet/Mvc/issues/5992
						.AddApplicationPart(Assembly.Load(new AssemblyName(nameof(SampleAspNetCoreApp))));
				})
				.UseUrls("http://localhost:5900") //CI doesn't like https, so we roll with http
				.Build()
				.RunAsync();
		}

		/// <summary>
		/// Integration test that makes sure that the ASP.NET Core auto instrumentation captures logged in users.
		/// It starts up the <see cref="SampleAspNetCoreApp" /> and creates a user
		/// then it logs in the user and then it sends an HTTP GET request to /Home/SimplePage (with the logged in user)
		/// It makes sure that this last transaction contains the user.
		/// This test uses the test app that is built on top of the default ASP.NET Core  <see cref="UserManager{TUser}" />
		/// and <see cref="UserManager{TUser}" />.
		/// </summary>
		[Fact]
		public async Task RegisterAndLogInUser()
		{
			SetUpSut();

			const string userName = "TestUser";
			const string password = "aaaaaa";


			//We need to ensure we are not propagating any unsampled current activities
			var client = new HttpClient(new DisableActivityHandler(_output)) { BaseAddress = new Uri("http://localhost:5900") };

			//Home/Index runs the migrations, so this makes sure the DB exists
			var homeResult = await client.GetAsync("/Home/Index");
			homeResult.IsSuccessStatusCode.Should().BeTrue();

			//create user
			var formContent = new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("username", userName), new KeyValuePair<string, string>("password", password)
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

			transactionWithUser
				.Context.User.Email.Should()
				.StartWith(userName);

			transactionWithUser
				.Context.User.Id.Should()
				.NotBeEmpty();
		}

		public void Dispose() => _agent?.Dispose();
	}
}
