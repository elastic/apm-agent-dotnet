using System;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.AspNetCore.Tests.Factories;
using Elastic.Apm.AspNetCore.Tests.Fakes;
using Elastic.Apm.AspNetCore.Tests.Helpers;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="DiagnosticListener.AspNetCoreDiagnosticListener" /> type.
	/// </summary>
	[Collection("DiagnosticListenerTest")] // To avoid tests from DiagnosticListenerTests running in parallel with this we add them to 1 collection.
	public class AspNetCoreDiagnosticListenerTest : IClassFixture<CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup>>
	{
		private readonly CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> _factory;

		public AspNetCoreDiagnosticListenerTest(CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> factory) => _factory = factory;

		/// <summary>
		/// Triggers /Home/TriggerError from the sample app and makes sure that the error is captured.
		/// </summary>
		/// <returns>The error in ASP.NET Core.</returns>
		[Fact]
		public async Task TestErrorInAspNetCore()
		{
			using (var agent = new ApmAgent(new TestAgentComponents()))
			{
				var capturedPayload = agent.PayloadSender as MockPayloadSender;
				var client = TestHelper.GetClient(_factory, agent);

				await client.GetAsync("/Home/TriggerError");

				capturedPayload.Should().NotBeNull();
				capturedPayload.Transactions.Should().ContainSingle();
				capturedPayload.Errors.Should().ContainSingle();

				var errorException = capturedPayload.FirstError.Exception;
				errorException.Message.Should().Be("This is a test exception!");
				errorException.Type.Should().Be(typeof(Exception).FullName);

				var context = capturedPayload.FirstError.Context;
				context.Request.Url.Full.Should().Be("http://localhost/Home/TriggerError");
				context.Request.Method.Should().Be(HttpMethod.Get.Method);

				errorException.Should().NotBeNull();
				errorException.Handled.Should().BeFalse();
			}
		}
	}
}
