using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="Elastic.Apm.AspNetCore.DiagnosticListener.AspNetCoreDiagnosticListener" /> type.
	/// </summary>
	[Collection("DiagnosticListenerTest")] //To avoid tests from DiagnosticListenerTests running in parallel with this we add them to 1 collection.
	public class AspNetCoreDiagnosticListenerTest : IClassFixture<WebApplicationFactory<Startup>>
	{
		private readonly WebApplicationFactory<Startup> _factory;

		public AspNetCoreDiagnosticListenerTest(WebApplicationFactory<Startup> factory) => _factory = factory;

		/// <summary>
		/// Triggers /Home/TriggerError from the sample app
		/// and makes sure that the error is captured.
		/// </summary>
		/// <returns>The error in ASP net core.</returns>
		[Fact]
		public async Task TestErrorInAspNetCore()
		{
			using (var agent = new ApmAgent(new TestAgentComponents()))
			{
				var capturedPayload = agent.PayloadSender as MockPayloadSender;
				var client = Helper.GetClient(agent, _factory);

				var response = await client.GetAsync("/Home/TriggerError");

				capturedPayload.Should().NotBeNull();
				capturedPayload.Transactions.Should().ContainSingle();;

				capturedPayload.Errors.Should().ContainSingle();

				var errorException = capturedPayload.Errors[0].Exception;
				errorException.Message.Should().Be("This is a test exception!");
				errorException.Type.Should().Be(typeof(Exception).FullName);

				var context = capturedPayload.FirstError.Context;
				context.Request.Url.Full.Should().Be("/Home/TriggerError");
				context.Request.Method.Should().Be(HttpMethod.Get.Method);

				errorException.Should().NotBeNull();
				errorException.Handled.Should().BeFalse();
			}
		}
	}
}
