using System;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mocks;
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

				Assert.Single(capturedPayload.Payloads);
				Assert.Single(capturedPayload.Payloads[0].Transactions);

				Assert.Single(capturedPayload.Errors);
				Assert.Single(capturedPayload.Errors[0].Errors);

				Assert.Equal("This is a test exception!", capturedPayload.Errors[0].Errors[0].Exception.Message);
				Assert.Equal(typeof(Exception).FullName, capturedPayload.Errors[0].Errors[0].Exception.Type);

				Assert.Equal("/Home/TriggerError", capturedPayload.FirstErrorDetail.Context.Request.Value.Url.Full);
				Assert.Equal(HttpMethod.Get.Method, capturedPayload.FirstErrorDetail.Context.Request.Value.Method);
				Assert.False((capturedPayload.FirstErrorDetail.Exception as CapturedException)?.Handled);
			}
		}
	}
}
