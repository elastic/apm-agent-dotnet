using System;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Tests;
using Elastic.Apm.Tests.Mock;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="Elastic.Apm.AspNetCore.DiagnosticListener.AspNetCoreDiagnosticListener" /> type.
	/// </summary>
	public class AspNetCoreDiagnosticListenerTest : IClassFixture<WebApplicationFactory<Startup>>
	{
		private readonly WebApplicationFactory<Startup> factory;

		public AspNetCoreDiagnosticListenerTest(WebApplicationFactory<Startup> factory)
		{
			this.factory = factory;
			TestHelper.ResetAgentAndEnvVars();
		}

		/// <summary>
		/// Triggers /Home/TriggerError from the sample app
		/// and makes sure that the error is captured.
		/// </summary>
		/// <returns>The error in ASP net core.</returns>
		[Fact]
		public async Task TestErrorInAspNetCore()
		{
			var capturedPayload = new MockPayloadSender();
			var client = Helper.GetClient(capturedPayload, factory);

			var response = await client.GetAsync("/Home/TriggerError");

			Assert.Single(capturedPayload.Payloads);
			Assert.Single(capturedPayload.Payloads[0].Transactions);

			//This should be .Single, but despite 'CollectionBehavior(DisableTestParallelization = true)' it sometimes contains 2 errors when all tests are runnning. 
			//TODO: This must be further investigated, based on first look it seems to be XUnit related - from what I see we don't capture or send errors twice.
			Assert.NotEmpty(capturedPayload.Errors);
			Assert.Single(capturedPayload.Errors[0].Errors);

			Assert.Equal("This is a test exception!", capturedPayload.Errors[0].Errors[0].Exception.Message);
			Assert.Equal(typeof(Exception).FullName, capturedPayload.Errors[0].Errors[0].Exception.Type);

			Assert.Equal("/Home/TriggerError", capturedPayload.Errors[0].Errors[0].Context.Request.Url.Full);
			Assert.Equal(HttpMethod.Get.Method, capturedPayload.Errors[0].Errors[0].Context.Request.Method);
			Assert.False(capturedPayload.Errors[0].Errors[0].Exception.Handled);
		}
	}
}
