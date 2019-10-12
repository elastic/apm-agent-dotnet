using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SampleAspNetCoreApp;
using SampleAspNetCoreApp.Controllers;
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
	public class BodyCapturingTests : IAsyncLifetime
	{
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		private readonly MockPayloadSender _payloadSender1 = new MockPayloadSender();
		private ApmAgent _agent;
		private Task _taskForSampleApp;

		/// <summary>
		/// Calls <see cref="HomeController.Send(BaseReportFilter{SendMessageFilter})" />.
		/// That method returns HTTP 500 in case the request body is null in the method, otherwise HTT 200.
		/// Tests against https://github.com/elastic/apm-agent-dotnet/issues/460
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task ComplexDataSendCaptureBody()
		{
			// build test data, which we send to the sample app
			var data = new BaseReportFilter<SendMessageFilter>();
			data.ReportFilter = new SendMessageFilter();
			data.ReportFilter.Body = "message body";
			data.ReportFilter.SenderApplicationCode = "26";
			data.ReportFilter.MediaType = "TokenBasedSms";
			data.ReportFilter.Recipients = new List<string> { "abc123" };

			var body = JsonConvert.SerializeObject(data,
				new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

			// send data to the sample app
			var httpClient = new HttpClient();
			httpClient.BaseAddress = new Uri("http://localhost:5903");
			var result = await httpClient.PostAsync("api/Home/Send", new StringContent(body, Encoding.UTF8, "application/json"));

			// make sure the sample app received the data
			result.StatusCode.Should().Be(200);

			// and make sure the data is captured by the agent
			_payloadSender1.FirstTransaction.Should().NotBeNull();
			_payloadSender1.FirstTransaction.Context.Request.Body.Should().Be(body);
		}

		public Task InitializeAsync()
		{
			_agent = new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender1,
				config: new MockConfigSnapshot(new NoopLogger(), captureBody: ConfigConsts.SupportedValues.CaptureBodyAll)));

			_taskForSampleApp = Program.CreateWebHostBuilder(null)
				.ConfigureServices(services =>
					{
						Startup.ConfigureServicesExceptMvc(services);

						services.AddMvc()
							.AddApplicationPart(Assembly.Load(new AssemblyName(nameof(SampleAspNetCoreApp))))
							.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
					}
				)
				.Configure(app =>
				{
					app.UseElasticApm(_agent, new TestLogger());
					Startup.ConfigureAllExceptAgent(app);
				})
				.UseUrls("http://localhost:5903")
				.Build()
				.RunAsync(_cancellationTokenSource.Token);

			return Task.CompletedTask;
		}

		public async Task DisposeAsync()
		{
			_cancellationTokenSource.Cancel();
			await _taskForSampleApp;

			_agent?.Dispose();
		}
	}
}
