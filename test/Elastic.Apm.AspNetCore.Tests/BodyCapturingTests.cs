using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.Extensions;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
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
		private const string MyCustomContentType = "application/my-custom-content";
		private SutEnv _sutEnv;

		public static IEnumerable<object[]> OptionsChangedAfterStartTestVariants =>
			BuildOptionsTestVariants().ZipWithIndex().Select(x => new object[] { x.Item1, x.Item2 });

		public Task InitializeAsync() => Task.CompletedTask;

		public Task DisposeAsync() => _sutEnv?.DisposeAsync();

		/// <summary>
		/// Calls <see cref="HomeController.Send(BaseReportFilter{SendMessageFilter})" />.
		/// That method returns HTTP 500 in case the request body is null in the method, otherwise HTTP 200.
		/// Tests against https://github.com/elastic/apm-agent-dotnet/issues/460
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task ComplexDataSendCaptureBody()
		{
			var sutEnv = StartSutEnv(new MockConfigSnapshot(new NoopLogger(), captureBody: ConfigConsts.SupportedValues.CaptureBodyAll));

			// build test data, which we send to the sample app
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

			var body = JsonConvert.SerializeObject(data,
				new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

			// send data to the sample app
			var result = await sutEnv.HttpClient.PostAsync("api/Home/Send", new StringContent(body, Encoding.UTF8, "application/json"));

			// make sure the sample app received the data
			result.StatusCode.Should().Be(200);

			// and make sure the data is captured by the agent
			sutEnv.MockPayloadSender.FirstTransaction.Should().NotBeNull();
			sutEnv.MockPayloadSender.FirstTransaction.Context.Request.Body.Should().Be(body);
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
					foreach (var isSampled in new[] { false, true})
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
		public async Task Options_changed_after_start(int startCfgVariantIndex, OptionsTestVariant startCfgVariant)
		{
			var startConfigSnapshot = new MockConfigSnapshot(new NoopLogger()
				, captureBody: startCfgVariant.CaptureBody
				, captureBodyContentTypes: startCfgVariant.CaptureBodyContentTypes
				, transactionSampleRate: startCfgVariant.IsSampled ? "1" : "0");
			var sutEnv = StartSutEnv(startConfigSnapshot);

			//
			// Verify that capture-body feature works as expected according to the initial configuration
			//

			foreach (var isError in new[] { false, true })
			{
				var body = new ToStringBuilder
				{
					{ nameof(startCfgVariantIndex), startCfgVariantIndex },
					{ nameof(startCfgVariant), startCfgVariant },
					{ nameof(isError), isError },
				}.ToString();

				await TestBodyCapture(body, isError, ShouldRequestBodyBeCaptured(startConfigSnapshot, isError), startCfgVariant.IsSampled);
			}

			//
			// Change the configuration and verify that capture-body feature as expected according to the updated configuration
			//

			await BuildOptionsTestVariants()
				.ForEachIndexed(async (updateCfgVariant, updateCfgVariantIndex) =>
				{
					var updateConfigSnapshot = new MockConfigSnapshot(
						new NoopLogger()
						, captureBody: updateCfgVariant.CaptureBody
						, captureBodyContentTypes: updateCfgVariant.CaptureBodyContentTypes
						, transactionSampleRate: updateCfgVariant.IsSampled ? "1" : "0"
					);

					sutEnv.Agent.ConfigStore.CurrentSnapshot = updateConfigSnapshot;

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

						await TestBodyCapture(body, isError, ShouldRequestBodyBeCaptured(updateConfigSnapshot, isError), updateCfgVariant.IsSampled);
					}
				});

			bool ShouldRequestBodyBeCaptured(IConfigurationReader configSnapshot, bool isError)
			{
				if (!configSnapshot.CaptureBodyContentTypes.Contains(MyCustomContentType)) return false;

				if (configSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyOff)) return false;
				if (configSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyAll)) return true;

				return isError || configSnapshot.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyTransactions);
			}

			// ReSharper disable once ImplicitlyCapturedClosure
			async Task TestBodyCapture(string body, bool isError, bool shouldRequestBodyBeCaptured, bool isSampled)
			{
				sutEnv.MockPayloadSender.Clear();

				var urlPath = isError ? "api/Home/PostError" : "api/Home/Post";
				HttpResponseMessage response = null;
				try
				{
					response = await sutEnv.HttpClient.PostAsync(urlPath, new StringContent(body, Encoding.UTF8, MyCustomContentType));
				}
				catch (Exception)
				{
					isError.Should().BeTrue();
				}

				sutEnv.MockPayloadSender.Transactions.Should().HaveCount(1);
				var transaction = sutEnv.MockPayloadSender.FirstTransaction;
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
					sutEnv.MockPayloadSender.Errors.Should().HaveCount(1);
					var error = sutEnv.MockPayloadSender.FirstError;
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
					sutEnv.MockPayloadSender.Errors.Should().BeEmpty();
				}
			}
		}

		private SutEnv StartSutEnv(IConfigSnapshot startConfig = null)
		{
			if (_sutEnv != null) return _sutEnv;

			_sutEnv = new SutEnv(startConfig);
			return _sutEnv;
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

		private class SutEnv
		{
			private const string UrlForTestApp = "http://localhost:5903";
			internal readonly ApmAgent Agent;
			internal readonly HttpClient HttpClient;
			internal readonly MockPayloadSender MockPayloadSender = new MockPayloadSender();

			private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
			private readonly Task _taskForSampleApp;

			internal SutEnv(IConfigSnapshot startConfig = null)
			{
				Agent = new ApmAgent(new TestAgentComponents(new NoopLogger(), startConfig, MockPayloadSender));

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
						app.UseElasticApm(Agent, new TestLogger());
						Startup.ConfigureAllExceptAgent(app);
					})
					.UseUrls(UrlForTestApp)
					.Build()
					.RunAsync(_cancellationTokenSource.Token);

				HttpClient = new HttpClient { BaseAddress = new Uri(UrlForTestApp) };
			}

			internal async Task DisposeAsync()
			{
				HttpClient.Dispose();

				_cancellationTokenSource.Cancel();
				await _taskForSampleApp;

				_cancellationTokenSource.Dispose();

				Agent?.Dispose();
			}
		}
	}
}
