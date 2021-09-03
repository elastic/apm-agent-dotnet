// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
			var sutEnv = StartSutEnv(new MockConfigurationSnapshot(new NoopLogger(), captureBody: ConfigConsts.SupportedValues.CaptureBodyAll));

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

		[Fact]
		public async Task Body_Capture_Should_Not_Error_When_Large_File()
		{
			var sutEnv = StartSutEnv(new MockConfigurationSnapshot(new NoopLogger(), captureBody: ConfigConsts.SupportedValues.CaptureBodyAll));

			using (var tempFile = new TempFile())
			{
				var content = $"Building a large file for testing!{Environment.NewLine}";
				var count = Encoding.UTF8.GetByteCount(content);
				var bytes = Encoding.UTF8.GetBytes(content);
				var repeat = (150 * 1024 * 1024) / count;

				// create a ~150Mb file for testing
				using (var stream = new FileStream(tempFile.Path, FileMode.OpenOrCreate, FileAccess.Write))
				{
					for (var i = 0; i < repeat; i++)
						stream.Write(bytes, 0, count);
				}

				HttpResponseMessage response;
				using (var formData = new MultipartFormDataContent
				{
					{ new StreamContent(new FileStream(tempFile.Path, FileMode.Open, FileAccess.Read)), "file", "file" }
				})
					response = await sutEnv.HttpClient.PostAsync("Home/File", formData);

				response.IsSuccessStatusCode.Should().BeTrue();
				var responseContent = int.Parse(await response.Content.ReadAsStringAsync());
				responseContent.Should().Be(repeat * count);
				sutEnv.MockPayloadSender.Transactions.Should().HaveCount(1);
				sutEnv.MockPayloadSender.Errors.Should().BeEmpty();
			}
		}

		[Fact]
		public async Task Body_Capture_Should_Capture_Stream()
		{
			var sutEnv = StartSutEnv(new MockConfigurationSnapshot(new NoopLogger(), captureBody: ConfigConsts.SupportedValues.CaptureBodyAll));

			var json = JsonConvert.SerializeObject(new { key1 = "value1" });
			var count = Encoding.UTF8.GetByteCount(json);

			HttpResponseMessage response;
			using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
				response = await sutEnv.HttpClient.PostAsync("Home/Stream", content);

			response.IsSuccessStatusCode.Should().BeTrue();
			var responseContent = int.Parse(await response.Content.ReadAsStringAsync());
			responseContent.Should().Be(count);

			sutEnv.MockPayloadSender.Transactions.Should().HaveCount(1);
			var transaction = sutEnv.MockPayloadSender.FirstTransaction;
			transaction.Context.Request.Body.Should().NotBeNull().And.BeOfType<string>();
			var body = (string)transaction.Context.Request.Body;
			body.Should().HaveLength(json.Length);
			sutEnv.MockPayloadSender.Errors.Should().BeEmpty();
		}

		[Fact]
		public async Task Body_Capture_Should_Capture_Stream_Up_To_MaxLength()
		{
			var sutEnv = StartSutEnv(new MockConfigurationSnapshot(new NoopLogger(), captureBody: ConfigConsts.SupportedValues.CaptureBodyAll));

			var jObject = new JObject();
			var charLength = 0;
			var i = 0;

			// an approximation, since doesn't include JSON syntax
			while (charLength < Consts.RequestBodyMaxLength)
			{
				var key = $"key{i}";
				var value = $"value{i}";
				charLength += key.Length + value.Length;
				jObject.Add(key, value);
				++i;
			}

			var json = jObject.ToString(Formatting.None);
			var count = Encoding.UTF8.GetByteCount(json);

			HttpResponseMessage response;
			using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
				response = await sutEnv.HttpClient.PostAsync("Home/Stream", content);

			response.IsSuccessStatusCode.Should().BeTrue();
			var responseContent = int.Parse(await response.Content.ReadAsStringAsync());
			responseContent.Should().Be(count);

			sutEnv.MockPayloadSender.Transactions.Should().HaveCount(1);
			var transaction = sutEnv.MockPayloadSender.FirstTransaction;
			transaction.Context.Request.Body.Should().NotBeNull().And.BeOfType<string>();
			var body = (string)transaction.Context.Request.Body;
			body.Should().HaveLength(Consts.RequestBodyMaxLength);
			sutEnv.MockPayloadSender.Errors.Should().BeEmpty();
		}

		[Fact]
		public async Task Body_Capture_Should_Capture_Form()
		{
			var sutEnv = StartSutEnv(new MockConfigurationSnapshot(new NoopLogger(), captureBody: ConfigConsts.SupportedValues.CaptureBodyAll));

			var formValues = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("key1", "value1"),
				new KeyValuePair<string, string>("key2", "value2"),
			};

			HttpResponseMessage response;
			using (var formData = new FormUrlEncodedContent(formValues))
				response = await sutEnv.HttpClient.PostAsync("Home/Form", formData);

			response.IsSuccessStatusCode.Should().BeTrue();
			sutEnv.MockPayloadSender.Transactions.Should().HaveCount(1);
			var transaction = sutEnv.MockPayloadSender.FirstTransaction;
			transaction.Context.Request.Body.Should().NotBeNull().And.BeOfType<string>();
			var body = (string)transaction.Context.Request.Body;
			body.Should().HaveLength("key1=value1&key2=value2".Length);
			sutEnv.MockPayloadSender.Errors.Should().BeEmpty();
		}

		[Fact]
		public async Task Body_Capture_Should_Capture_Form_Up_To_MaxLength()
		{
			var sutEnv = StartSutEnv(new MockConfigurationSnapshot(new NoopLogger(), captureBody: ConfigConsts.SupportedValues.CaptureBodyAll));

			var charLength = 0;
			var formValues = new List<KeyValuePair<string, string>>();
			var i = 0;

			while (charLength < Consts.RequestBodyMaxLength)
			{
				var key = $"key{i}";
				var value = $"value{i}";
				charLength += key.Length + value.Length;
				formValues.Add(new KeyValuePair<string, string>(key, value));
				++i;
			}

			HttpResponseMessage response;
			using (var formData = new FormUrlEncodedContent(formValues))
				response = await sutEnv.HttpClient.PostAsync("Home/Form", formData);

			response.IsSuccessStatusCode.Should().BeTrue();
			sutEnv.MockPayloadSender.Transactions.Should().HaveCount(1);
			var transaction = sutEnv.MockPayloadSender.FirstTransaction;
			transaction.Context.Request.Body.Should().NotBeNull().And.BeOfType<string>();
			var body = (string)transaction.Context.Request.Body;
			body.Should().HaveLength(Consts.RequestBodyMaxLength);
			sutEnv.MockPayloadSender.Errors.Should().BeEmpty();
		}

		[Fact]
		public async Task ApmMiddleware_ShouldSkipCapturing_WhenInvalidContentType()
		{
			// Arrange
			var sutEnv = StartSutEnv(new MockConfigurationSnapshot(new NoopLogger(), captureBody: ConfigConsts.SupportedValues.CaptureBodyErrors));

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

			var content = new StringContent(body, Encoding.UTF8);
			content.Headers.Clear();
			content.Headers.TryAddWithoutValidation("Content-Type", "123");

			// Act
			var result = await sutEnv.HttpClient.PostAsync("api/Home/Send", content);

			// Assert
			result.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
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
							CaptureBody = captureBody, CaptureBodyContentTypes = captureBodyContentTypes, IsSampled = isSampled
						};
					}
				}
			}
		}

		[Theory]
		[MemberData(nameof(OptionsChangedAfterStartTestVariants))]
		public async Task Options_changed_after_start(int startCfgVariantIndex, OptionsTestVariant startCfgVariant)
		{
			var startConfigSnapshot = new MockConfigurationSnapshot(new NoopLogger()
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
					var updateConfigSnapshot = new MockConfigurationSnapshot(
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

		private SutEnv StartSutEnv(IConfigurationSnapshot startConfiguration = null)
		{
			if (_sutEnv != null) return _sutEnv;

			_sutEnv = new SutEnv(startConfiguration);
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

			internal SutEnv(IConfigurationSnapshot startConfiguration = null)
			{
				Agent = new ApmAgent(new TestAgentComponents(new NoopLogger(), startConfiguration, MockPayloadSender));

				_taskForSampleApp = Program.CreateWebHostBuilder(null)
					.ConfigureServices(services =>
						{
							services.Configure<KestrelServerOptions>(options => {  });
							Startup.ConfigureServicesExceptMvc(services);

							services.AddMvc()
								.AddApplicationPart(Assembly.Load(new AssemblyName(nameof(SampleAspNetCoreApp))));
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
