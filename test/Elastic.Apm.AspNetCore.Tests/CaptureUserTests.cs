using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore.Tests.Factories;
using Elastic.Apm.AspNetCore.Tests.Fakes;
using Elastic.Apm.AspNetCore.Tests.Helpers;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// The ASP.NET Core auto instrumentation is able to capture logged in users. This class tests that feature.
	/// </summary>
	[Collection("DiagnosticListenerTest")]
	public class CaptureUserTests : IDisposable, IClassFixture<CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup>>
	{
		private readonly CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> _factory;

		public CaptureUserTests(CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> factory) => _factory = factory;

		/// <summary>
		/// Integration test that makes sure that the ASP.NET Core auto instrumentation captures logged in users.
		/// It starts up the <see cref="AspNetCoreSampleApp" /> and creates a user
		/// then it logs in the user and then it sends an HTTP GET request to /Home/SimplePage (with the logged in user)
		/// It makes sure that this last transaction contains the user.
		/// This test uses the test app that is built on top of the default ASP.NET Core  <see cref="UserManager{TUser}" />
		/// and <see cref="UserManager{TUser}" />.
		/// </summary>
		[Fact]
		public async Task RegisterAndLogInUser()
		{
			const string userName = "TestUser";
			const string password = "aaaaaa";

			using (var agent = GetAgent())
			{
				using (var client = TestHelper.GetClient(_factory, agent))
				{
					// Home/Index runs the migrations, so this makes sure the DB exists.
					var result = await client.GetAsync("/Home/Index");
					result.IsSuccessStatusCode.Should().BeTrue();

					// Create user.
					var formContent = new FormUrlEncodedContent(new[]
					{
						new KeyValuePair<string, string>("username", userName),
						new KeyValuePair<string, string>("password", password)
					});

					await client.PostAsync("/Account/RegisterUser", formContent);

					// Login.
					var loginResult = await client.PostAsync("/Account/LoginUser", formContent);
					loginResult.IsSuccessStatusCode.Should().BeTrue();

					// Get /Home/SimplePage with the user.
					var simplePage = await client.GetAsync("/Home/SimplePage");
					simplePage.IsSuccessStatusCode.Should().BeTrue();
				}

				var transactionWithUser = ((MockPayloadSender)agent.PayloadSender).Transactions.OrderByDescending(n => (n as Transaction)?.Timestamp).First();
				transactionWithUser.Context.User.UserName.Should().Be(userName);
			}
		}

		/// <summary>
		/// A unit test that directly calls InvokeAsync on the <see cref="ApmMiddleware" />.
		/// It tests for OpenID claims.
		/// It creates a <see cref="DefaultHttpContext" /> with email and sub claims on it (those are OpenID standard)
		/// and make sure that the agent captured the userid and the email address of the user.
		/// </summary>
		[Fact]
		public async Task OpenIdClaimsTest()
		{
			const string mail = "my@mail.com";
			const string sub = "123-456";

			using (var agent = GetAgent())
			{
				var context = new DefaultHttpContext
				{
					User = new ClaimsPrincipal(new ClaimsIdentity(new[]
					{
						new Claim("email", mail),
						new Claim("sub", sub)
					}, "someAuthTypeName"))
				};

				await new ApmMiddleware(async innerHttpContext => { await Task.Delay(1); }, agent.Tracer as Tracer, agent).InvokeAsync(context);

				var capturedPayload = (MockPayloadSender)agent.PayloadSender;
				capturedPayload.FirstTransaction.Context.User.Email.Should().Be(mail);
				capturedPayload.FirstTransaction.Context.User.Id.Should().Be(sub);
			}
		}

		private static ApmAgent GetAgent() => new ApmAgent(new TestAgentComponents(payloadSender: new MockPayloadSender()));

		public void Dispose() => _factory.Dispose();
	}
}
