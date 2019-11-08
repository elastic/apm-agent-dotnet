using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using AspNetCoreSampleApp;
using AspNetCoreSampleApp.Controllers;
using Elastic.Apm.AspNetCore.Tests.Factories;
using Elastic.Apm.AspNetCore.Tests.Fakes;
using Elastic.Apm.AspNetCore.Tests.Helpers;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the capture body feature of ASP.NET Core
	/// During investigation of https://github.com/elastic/apm-agent-dotnet/issues/460
	/// it turned out that tests that host the sample application with <see cref="IClassFixture{TFixture}" />
	/// don't reproduce the problem reported in #460.
	/// This test uses <see cref="Program.CreateWebHostBuilder" /> to host the sample application on purpose - that was the key
	/// to reproduce the problem in an automated test.
	/// </summary>
	[Collection("DiagnosticListenerTest")] //To avoid tests from DiagnosticListenerTests running in parallel with this we add them to 1 collection.
	public class BodyCapturingTests : IClassFixture<CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup>>
	{
		private readonly CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> _factory;

		public BodyCapturingTests(CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> factory) => _factory = factory;

		/// <summary>
		/// Calls <see cref="HomeController.Send(BaseReportFilter{SendMessageFilter})" />.
		/// That method returns HTTP 500 in case the request body is null in the method, otherwise HTT 200.
		/// Tests against https://github.com/elastic/apm-agent-dotnet/issues/460
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task ComplexDataSendCaptureBody()
		{
			// Build test data, which we send to the sample app.
			var data = new BaseReportFilter<SendMessageFilter>
			{
				ReportFilter = new SendMessageFilter
				{
					Body = "message body",
					SenderApplicationCode = "26",
					MediaType = "TokenBasedSms",
					Recipients = new List<string> { "abc123" }
				}
			};

			var body = JsonConvert.SerializeObject(data, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

			// Send data to the sample app.
			using (var agent = GetAgent())
			using (var client = TestHelper.GetClient(_factory, agent))
			{
				client.BaseAddress = new Uri("http://localhost:5903");
				using (var result = await client.PostAsync("api/Home/Send", new StringContent(body, Encoding.UTF8, "application/json")))
				{
					// Make sure the sample app received the data.
					result.StatusCode.Should().Be(200);

					var capturedPayload = (MockPayloadSender)agent.PayloadSender;
					capturedPayload.FirstTransaction.Should().NotBeNull();
					capturedPayload.FirstTransaction.Context.Request.Body.Should().Be(body);
				}
			}
		}

		private static ApmAgent GetAgent() => new ApmAgent(new TestAgentComponents(
			payloadSender: new MockPayloadSender(),
			config: new MockConfigSnapshot(new NoopLogger(), captureBody: ConfigConsts.SupportedValues.CaptureBodyAll)));
	}
}
