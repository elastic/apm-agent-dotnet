using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AspNetCoreSampleApp;
using AspNetCoreSampleApp.Controllers;
using Elastic.Apm.AspNetCore.Tests.Factories;
using Elastic.Apm.AspNetCore.Tests.Fakes;
using Elastic.Apm.AspNetCore.Tests.Helpers;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.Extensions;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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

		private const string MyCustomContentType = "application/my-custom-content";

		public static IEnumerable<object[]> OptionsChangedAfterStartTestVariants =>
			BuildOptionsTestVariants().ZipWithIndex().Select(x => new object[] { x.Item1, x.Item2 });

		public BodyCapturingTests(CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup> factory) => _factory = factory;

		/// <summary>
		/// Calls <see cref="HomeController.Send(BaseReportFilter{SendMessageFilter})" />.
		/// That method returns HTTP 500 in case the request body is null in the method, otherwise HTTP 200.
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
			using (var agent = GetAgent(new MockConfigSnapshot(new NoopLogger(), captureBody: ConfigConsts.SupportedValues.CaptureBodyAll)))
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

		private static IEnumerable<OptionsTestVariant> BuildOptionsTestVariants()
		{
			var captureBodyContentTypesVariants = new[]
			{
				ConfigConsts.DefaultValues.CaptureBodyContentTypes,
				ConfigConsts.DefaultValues.CaptureBodyContentTypes + ", " + MyCustomContentType
			};

			foreach (var captureBody in ConfigConsts.SupportedValues.CaptureBodySupportedValues)
			{
				foreach (var captureBodyContentTypes in captureBodyContentTypesVariants)
				{
					foreach (var isSampled in new[] { false, true })
					{
						yield return new OptionsTestVariant
						{
							CaptureBody = captureBody,
							CaptureBodyContentTypes = captureBodyContentTypes,
							IsSampled = isSampled
						};
					}
				}
			}
		}

		[Theory]
		[MemberData(nameof(OptionsChangedAfterStartTestVariants))]
		public async Task OptionsChangedAfterStart(int startCfgVariantIndex, OptionsTestVariant startCfgVariant)
		{
			var startConfigSnapshot = new MockConfigSnapshot(new NoopLogger(),
				captureBody: startCfgVariant.CaptureBody,
				captureBodyContentTypes: startCfgVariant.CaptureBodyContentTypes,
				transactionSampleRate: startCfgVariant.IsSampled ? "1" : "0");

			//
			// Verify that capture-body feature works as expected according to the initial configuration.
			//

			using (var agent = GetAgent(startConfigSnapshot))
			using (var client = TestHelper.GetClient(_factory, agent))
			{
				client.BaseAddress = new Uri("http://localhost:5903");

				foreach (var isError in new[] { false, true })
				{
					var body = new ToStringBuilder
					{
						{ nameof(startCfgVariantIndex), startCfgVariantIndex },
						{ nameof(startCfgVariant), startCfgVariant },
						{ nameof(isError), isError }
					}.ToString();

					await TestBodyCapture(agent, client, body, isError, ShouldRequestBodyBeCaptured(startConfigSnapshot, isError), startCfgVariant.IsSampled);
				}

				//
				// Change the configuration and verify that capture-body feature as expected according to the updated configuration
				//

				await BuildOptionsTestVariants().ForEachIndexed(async (updateCfgVariant, updateCfgVariantIndex) =>
				{
					var updateConfigSnapshot = new MockConfigSnapshot(
						new NoopLogger()
						, captureBody: updateCfgVariant.CaptureBody
						, captureBodyContentTypes: updateCfgVariant.CaptureBodyContentTypes
						, transactionSampleRate: updateCfgVariant.IsSampled ? "1" : "0"
					);

					agent.ConfigStore.CurrentSnapshot = updateConfigSnapshot;

					foreach (var isError in new[] { false, true })
					{
						var body = new ToStringBuilder
						{
							{ nameof(startCfgVariantIndex), startCfgVariantIndex },
							{ nameof(startCfgVariant), startCfgVariant },
							{ nameof(updateCfgVariantIndex), updateCfgVariantIndex },
							{ nameof(updateCfgVariant), updateCfgVariant },
							{ nameof(isError), isError },
						}.ToString();

						await TestBodyCapture(agent, client, body, isError, ShouldRequestBodyBeCaptured(updateConfigSnapshot, isError), updateCfgVariant.IsSampled);
					}
				});

				bool ShouldRequestBodyBeCaptured(IConfigurationReader configSnapshot, bool isError)
				{
					if (!configSnapshot.CaptureBodyContentTypes.Contains(MyCustomContentType)) return false;

					if (configSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyOff)) return false;
					if (configSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyAll)) return true;

					return isError || configSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyTransactions);
				}

				async Task TestBodyCapture(IApmAgent localAgent, HttpClient localClient, string body, bool isError, bool shouldRequestBodyBeCaptured, bool isSampled)
				{
					var capturedPayload = (MockPayloadSender)localAgent.PayloadSender;
					capturedPayload.Clear();

					var urlPath = isError ? "api/Home/PostError" : "api/Home/Post";
					HttpResponseMessage response = null;

					Func<Task<HttpResponseMessage>> func = async () => await localClient.PostAsync(urlPath, new StringContent(body, Encoding.UTF8, MyCustomContentType));;

					if (isError)
					{
						func.Should().Throw<Exception>();
					}
					else
					{
						response = await func();
					}

					capturedPayload.Transactions.Should().ContainSingle();
					var transaction = capturedPayload.FirstTransaction;
					object capturedRequestBody = null;
					transaction.IsSampled.Should().Be(isSampled);
					transaction.IsContextCreated.Should().Be(isSampled, $"body: {body}");
					if (isSampled)
					{
						capturedRequestBody = transaction.Context.Request.Body;
						capturedRequestBody.Should().Be(shouldRequestBodyBeCaptured ? body : Elastic.Apm.Consts.Redacted);
					}

					if (isError)
					{
						capturedPayload.Errors.Should().ContainSingle();
						var error = capturedPayload.FirstError;
						if (isSampled)
						{
							error.Transaction.IsSampled.Should().BeTrue();
							error.Context.Should().NotBeNull();
							error.Context.Request.Body.Should().Be(capturedRequestBody);
						}
						else
						{
							error.Transaction.IsSampled.Should().BeFalse();
							error.Context.Should().BeNull();
						}
					}
					else
					{
						var responseBody = await response.Content.ReadAsStringAsync();
						responseBody.Should().NotBeNull().And.Be(HomeController.PostResponseBody);
						capturedPayload.Errors.Should().BeEmpty();
					}

					response.Dispose();
				}
			}
		}

		public class OptionsTestVariant
		{
			internal string CaptureBody { get; set; }
			internal string CaptureBodyContentTypes { get; set; }
			internal bool IsSampled { get; set; }

			public override string ToString() => new ToStringBuilder(nameof(OptionsTestVariant))
			{
				{ nameof(CaptureBody), CaptureBody },
				{ nameof(CaptureBodyContentTypes), CaptureBodyContentTypes },
				{ nameof(IsSampled), IsSampled }
			}.ToString();
		}

		private static ApmAgent GetAgent(IConfigSnapshot config) =>
			new ApmAgent(new TestAgentComponents(payloadSender: new MockPayloadSender(), config: config));
	}
}
