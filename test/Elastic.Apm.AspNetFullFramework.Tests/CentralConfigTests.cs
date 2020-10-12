// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AspNetFullFrameworkSampleApp.Controllers;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class CentralConfigTests : TestsBase
	{
		private const string CustomEnvironment = "AspNetFullFramework_Tests_CentralConfigTests-CustomEnvironment";
		private const string CustomServiceName = "AspNetFullFramework_Tests_CentralConfigTests-CustomServiceName";
		private const string CustomHostName = "AspNetFullFramework_Tests_CentralConfigTests-CustomHostName";

		private const string TransactionMaxSpansKey = "transaction_max_spans";
		private const string TransactionSampleRateKey = "transaction_sample_rate";

		protected readonly ConfigStateC ConfigState;

		public CentralConfigTests(ITestOutputHelper xUnitOutputHelper)
			: base(
				xUnitOutputHelper,
				envVarsToSetForSampleAppPool: new Dictionary<string, string>
				{
					{ ConfigConsts.EnvVarNames.ServiceName, CustomServiceName },
					{ ConfigConsts.EnvVarNames.Environment, CustomEnvironment },
					{ ConfigConsts.EnvVarNames.HostName, CustomHostName },
				})
		{
			ConfigState = new ConfigStateC(LoggerBase);

			AgentConfig.ServiceName = AbstractConfigurationReader.AdaptServiceName(CustomServiceName);
			AgentConfig.Environment = CustomEnvironment;
			AgentConfig.HostName = CustomHostName;
		}

		[Collection(Consts.AspNetFullFrameworkTestsCollection)]
		public class MaxSpansAndSampleRateTests : CentralConfigTests
		{
			public MaxSpansAndSampleRateTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper)
			{
				// ReSharper disable once SwitchStatementMissingSomeCases
				switch (CurrentXunitTest.TestCase.TestMethod.Method.Name)
				{
					case nameof(MaxSpans_valid_value):
					{
						CurrentXunitTest.TestCase.TestMethodArguments.Length.Should().Be(1);
						var transactionMaxSpansLocalConfig = (int?)CurrentXunitTest.TestCase.TestMethodArguments[0];
						if (transactionMaxSpansLocalConfig.HasValue)
							EnvVarsToSetForSampleAppPool[ConfigConsts.EnvVarNames.TransactionMaxSpans] = transactionMaxSpansLocalConfig.ToString();
						break;
					}

					case nameof(SampleRate_valid_value):
					{
						CurrentXunitTest.TestCase.TestMethodArguments.Length.Should().Be(1);
						var transactionSampleRateLocalConfig = (bool?)CurrentXunitTest.TestCase.TestMethodArguments[0];
						if (transactionSampleRateLocalConfig.HasValue)
						{
							EnvVarsToSetForSampleAppPool[ConfigConsts.EnvVarNames.TransactionSampleRate] =
								transactionSampleRateLocalConfig.Value ? "1" : "0";
						}
						break;
					}

					case nameof(Combination_of_both_options):
					{
						CurrentXunitTest.TestCase.TestMethodArguments.Length.Should().Be(2);
						var transactionMaxSpansLocalConfig = (int?)CurrentXunitTest.TestCase.TestMethodArguments[0];
						var transactionSampleRateLocalConfig = (bool?)CurrentXunitTest.TestCase.TestMethodArguments[1];
						if (transactionMaxSpansLocalConfig.HasValue)
							EnvVarsToSetForSampleAppPool[ConfigConsts.EnvVarNames.TransactionMaxSpans] = transactionMaxSpansLocalConfig.ToString();
						if (transactionSampleRateLocalConfig.HasValue)
						{
							EnvVarsToSetForSampleAppPool[ConfigConsts.EnvVarNames.TransactionSampleRate] =
								transactionSampleRateLocalConfig.Value ? "1" : "0";
						}
						break;
					}

					case nameof(SampleRate_invalid_value):
					{
						EnvVarsToSetForSampleAppPool[ConfigConsts.EnvVarNames.TransactionSampleRate] = "0";
						break;
					}

					case nameof(MaxSpans_invalid_value):
					{
						EnvVarsToSetForSampleAppPool[ConfigConsts.EnvVarNames.TransactionMaxSpans] =
							MaxSpansLocalConfigForInvalidValueTest.ToString();
						break;
					}
				}
			}

			[AspNetFullFrameworkTheory]
			[InlineData(null)]
			[InlineData(false)]
			[InlineData(true)]
			public async Task SampleRate_valid_value(bool? isSampledLocalConfig) =>
				await TestImpl(
					new[]
					{
						new TestParams{ SpansToExecCount = 1, SampleRateCfg = "0" },
						new TestParams{ SpansToExecCount = 2, SampleRateCfg = "0" },
						new TestParams{ SpansToExecCount = 3, SampleRateCfg = "1" },
						new TestParams{ SpansToExecCount = 1 },
						new TestParams{ SpansToExecCount = 2 },
						new TestParams{ SpansToExecCount = 3, SampleRateCfg = "1" },
						new TestParams{ SpansToExecCount = 1, SampleRateCfg = "1" },
						new TestParams{ SpansToExecCount = 2, SampleRateCfg = "0" },
						new TestParams{ SpansToExecCount = 3 },
					}
					// Local config is set in ctor of this tests class.
					, isSampledLocalConfig: isSampledLocalConfig
					, spansToExecCountForInitialRequest: 4);

			[AspNetFullFrameworkTheory]
			[InlineData(null)]
			[InlineData(10)]
			[InlineData(40)]
			public async Task MaxSpans_valid_value(int? maxSpansLocalConfig) =>
				await TestImpl(
					new[]
					{
						new TestParams{ SpansToExecCount = 0, MaxSpansCfg = "0" },
						new TestParams{ SpansToExecCount = 0 },
						new TestParams{ SpansToExecCount = 1, MaxSpansCfg = "0" },
						new TestParams{ SpansToExecCount = 1, MaxSpansCfg = "1" },
						new TestParams{ SpansToExecCount = 1, MaxSpansCfg = "2" },
						new TestParams{ SpansToExecCount = 2, MaxSpansCfg = "2" },
						new TestParams{ SpansToExecCount = 2, MaxSpansCfg = "1" },
						new TestParams{ SpansToExecCount = 27 },
						new TestParams{ SpansToExecCount = 27, MaxSpansCfg = "0" },
						new TestParams{ SpansToExecCount = 27, MaxSpansCfg = "1" },
						new TestParams{ SpansToExecCount = 27, MaxSpansCfg = "15" },
						new TestParams{ SpansToExecCount = 27, MaxSpansCfg = "26" },
						new TestParams{ SpansToExecCount = 27, MaxSpansCfg = "27" },
						new TestParams{ SpansToExecCount = 27, MaxSpansCfg = "28" },
						new TestParams{ SpansToExecCount = 27, MaxSpansCfg = "30" },
					}
					// Local config is set in ctor of this tests class.
					, maxSpansLocalConfig: maxSpansLocalConfig
					, spansToExecCountForInitialRequest: 30);

			[AspNetFullFrameworkTheory]
			[InlineData(null, null)]
			[InlineData(10, false)]
			[InlineData(10, null)]
			public async Task Combination_of_both_options(int? maxSpansLocalConfig, bool? isSampledLocalConfig) =>
				await TestImpl(
					new[]
					{
						new TestParams{ SpansToExecCount = 25, MaxSpansCfg = "20", SampleRateCfg = "0" },
						new TestParams{ SpansToExecCount = 25, MaxSpansCfg = "20", SampleRateCfg = "1" },
						new TestParams{ SpansToExecCount = 25,                     SampleRateCfg = "1" },
						new TestParams{ SpansToExecCount = 15, MaxSpansCfg = "30", SampleRateCfg = "0" },
						new TestParams{ SpansToExecCount = 35, MaxSpansCfg = "30", SampleRateCfg = "1" },
					}
					// Local config is set in ctor of this tests class.
					, maxSpansLocalConfig: maxSpansLocalConfig
					, isSampledLocalConfig: isSampledLocalConfig
					, spansToExecCountForInitialRequest: 30);

			private const int MaxSpansLocalConfigForInvalidValueTest = ConfigConsts.DefaultValues.TransactionMaxSpans + 10;

			[AspNetFullFrameworkFact]
			public async Task MaxSpans_invalid_value() =>
				await TestImpl(
					new[]
					{
						new TestParams
						{
							SpansToExecCount = MaxSpansLocalConfigForInvalidValueTest + 2
							, MaxSpansCfg = "invalid TransactionMaxSpans value"
						}
					}
					// Local config is set to TransactionMaxSpansLocalConfigForInvalidValueTest in ctor of this tests class.
					, maxSpansLocalConfig: MaxSpansLocalConfigForInvalidValueTest
					, spansToExecCountForInitialRequest: MaxSpansLocalConfigForInvalidValueTest + 1);

			[AspNetFullFrameworkFact]
			public async Task SampleRate_invalid_value() =>
				await TestImpl(
					new[] { new TestParams { SpansToExecCount = 3, SampleRateCfg = "invalid TransactionSampleRate value" } }
					// TransactionSampleRate in local config is set to "0" in ctor of this tests class.
					, isSampledLocalConfig: false
					, spansToExecCountForInitialRequest: 2);

			private class TestParams
			{
				internal int SpansToExecCount { get; set; }
				internal string MaxSpansCfg { get; set; }
				internal string SampleRateCfg { get; set; }
			}

			private async Task TestImpl(
				IReadOnlyCollection<TestParams> testParamsPerStep
				, int spansToExecCountForInitialRequest
				, int? maxSpansLocalConfig = null
				, bool? isSampledLocalConfig = null)
			{
				var srcPageData = SampleAppUrlPaths.GenNSpansPage;
				// AssertReceivedDataSampledStatus(receivedData, isSampled, pageData.SpansCount); below relies on number of transactions being 1
				srcPageData.TransactionsCount.Should().Be(1);

				var maxSpansLocalConfigValue = maxSpansLocalConfig ?? ConfigConsts.DefaultValues.TransactionMaxSpans;
				var isSampledLocalConfigValue = isSampledLocalConfig ?? true;

				MockApmServer.GetAgentsConfig = (httpRequest, httpResponse) => ConfigState.BuildResponse(httpRequest, httpResponse);

				// Send first request before updating central configuration to make sure application is running
				await SendRequestAssertReceivedData(maxSpansLocalConfigValue, isSampledLocalConfigValue, spansToExecCountForInitialRequest);

				await testParamsPerStep.ForEach(async testParams =>
				{
					ClearState();

					await ConfigState.UpdateAndWaitForAgentToApply(new Dictionary<string, string>
					{
						{ TransactionMaxSpansKey, testParams.MaxSpansCfg },
						{ TransactionSampleRateKey, testParams.SampleRateCfg }
					});

					var maxSpans = testParams.MaxSpansCfg == null
						? maxSpansLocalConfigValue
						: int.TryParse(testParams.MaxSpansCfg, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxSpansCfgParsed)
							? maxSpansCfgParsed
							// If option value in central config is invalid then agent should fall back on default and not local config
							: ConfigConsts.DefaultValues.TransactionMaxSpans;

					var isSampled = testParams.SampleRateCfg == null ? isSampledLocalConfigValue : testParams.SampleRateCfg != "0";

					await SendRequestAssertReceivedData(maxSpans, isSampled, testParams.SpansToExecCount);
				});

				async Task SendRequestAssertReceivedData(int maxSpans, bool isSampled, int spansToExecCount)
				{
					var urlPath = srcPageData.RelativeUrlPath + $"?{HomeController.NumberOfSpansQueryStringKey}={spansToExecCount}";
					var pageData = srcPageData.Clone(urlPath, spansCount: isSampled ? Math.Min(spansToExecCount, maxSpans) : 0);
					await SendGetRequestToSampleAppAndVerifyResponse(pageData.RelativeUrlPath, pageData.StatusCode);

					await WaitAndCustomVerifyReceivedData(receivedData =>
					{
						VerifyReceivedDataSharedConstraints(pageData, receivedData);

						// Relies on srcPageData.TransactionsCount.Should().Be(1);
						AssertReceivedDataSampledStatus(receivedData, isSampled, pageData.SpansCount);

						if (isSampled) receivedData.Transactions.First().SpanCount.Dropped.Should().Be(spansToExecCount - pageData.SpansCount);

						receivedData.Spans.ForEachIndexed((span, i) =>
						{
							span.Name.Should().Be($"Span_#{i}_name");
							span.Type.Should().Be($"Span_#{i}_type");
						});
					},  /* shouldGatherDiagnostics: */ false);
				}
			}
		}

		protected class ConfigStateC
		{
			private const string ThisClassName = nameof(CentralConfigTests) + "." + nameof(ConfigStateC);

			private readonly object _lock = new object();

			private readonly IApmLogger _logger;

			private readonly Dictionary<string, string> _options = new Dictionary<string, string>();

			internal ConfigStateC(IApmLogger logger)
			{
				_logger = logger.Scoped(ThisClassName);
				_appliedByAgent = new TaskCompletionSource<object>();
			}

			private TaskCompletionSource<object> _appliedByAgent;

			private int _eTagAsInt = -1;
			private TimeSpan _waitUntilNextRequest = TimeSpan.FromSeconds(1);

			private (string, TaskCompletionSource<object>) Update(
				IReadOnlyDictionary<string, string> optionsToUpdate
				, TimeSpan? waitUntilNextRequest = null
			)
			{
				lock (_lock)
				{
					if (waitUntilNextRequest.HasValue) _waitUntilNextRequest = waitUntilNextRequest.Value;
					// ReSharper disable once UseDeconstructionOnParameter
					optionsToUpdate.ForEach(optToUpdate =>
					{
						if (optToUpdate.Value == null)
							_options.Remove(optToUpdate.Key);
						else
							_options[optToUpdate.Key] = optToUpdate.Value;
					});
					++_eTagAsInt;
					_appliedByAgent.TrySetCanceled();
					_appliedByAgent = new TaskCompletionSource<object>();
					return ($"[ETag: {EtagToString(_eTagAsInt)}] {JsonConvert.SerializeObject(_options)}", _appliedByAgent);
				}
			}

			internal async Task UpdateAndWaitForAgentToApply(
				IReadOnlyDictionary<string, string> optionsToUpdate
				, TimeSpan? waitUntilNextRequest = null
			)
			{
				var (dbgConfig, agentAppliedEvent) = Update(optionsToUpdate, waitUntilNextRequest);
				_logger.Debug()
					?.Log("Waiting for agent to apply updated central configuration..."
						+ " Central config options: {CentralConfigOptions}", dbgConfig);
				await agentAppliedEvent.Task;
			}

			internal IActionResult BuildResponse(HttpRequest httpRequest, HttpResponse httpResponse)
			{
				lock (_lock)
				{
					try
					{
						return BuildResponseImpl(httpRequest, httpResponse);
					}
					catch (Exception ex)
					{
						_logger.Error()?.LogException(ex, nameof(BuildResponseImpl) + " thrown exception");
						_appliedByAgent.SetException(ex);
						throw;
					}
				}
			}

			private static string EtagToString(int eTag) => $"\"{eTag}\"";

			private IActionResult BuildResponseImpl(HttpRequest httpRequest, HttpResponse httpResponse)
			{
				httpRequest.Query["service.name"].Should().HaveCount(1);
				httpRequest.Query["service.name"].First().Should().Be(CustomServiceName);
				httpRequest.Query["service.environment"].Should().HaveCount(1);
				httpRequest.Query["service.environment"].First().Should().Be(CustomEnvironment);

				var eTag = EtagToString(_eTagAsInt);

				httpResponse.Headers[HeaderNames.CacheControl] = "must-revalidate, max-age=" + _waitUntilNextRequest.TotalSeconds;
				httpResponse.Headers[HeaderNames.ETag] = eTag;

				var containsCurrentEtag = httpRequest.Headers[HeaderNames.IfNoneMatch]
					.SelectMany(values => values.Split(",").Select(val => val.Trim()))
					.Contains(eTag);
				// ReSharper disable once InvertIf
				if (containsCurrentEtag)
				{
					_appliedByAgent.TrySetResult(null);
					return new StatusCodeResult((int)HttpStatusCode.NotModified);
				}

				return new ObjectResult(JsonConvert.SerializeObject(_options)) { StatusCode = (int)HttpStatusCode.OK };
			}
		}
	}
}
