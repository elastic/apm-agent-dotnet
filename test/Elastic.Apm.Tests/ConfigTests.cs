// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Tests.Data;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Config.ConfigConsts;

namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Tests the configuration through environment variables
	/// </summary>
	[Collection("UsesEnvironmentVariables")]
	public class ConfigTests : IDisposable
	{
		public static TheoryData GlobalLabelsValidVariantsToTest => new TheoryData<string, IReadOnlyDictionary<string, string>>
		{
			// empty string - zero key value pairs
			{ "", new Dictionary<string, string>() },

			// one key and value pair where key and value are empty strings
			{ "=", new Dictionary<string, string> { { "", "" } } },

			// key is an empty string
			{ "=v", new Dictionary<string, string> { { "", "v" } } },

			// value is an empty string
			{ "k=", new Dictionary<string, string> { { "k", "" } } },

			// key and value are empty strings in the first pair
			{ "=,k=v", new Dictionary<string, string> { { "", "" }, { "k", "v" } } },

			// key and value are empty strings in the last pair
			{ "k=,=", new Dictionary<string, string> { { "k", "" }, { "", "" } } },

			// key and value are empty strings in the middle pair
			{ "key1=value1,=,key3=value3", new Dictionary<string, string> { { "key1", "value1" }, { "", "" }, { "key3", "value3" } } }
		};

		[Fact]
		public void ServerUrlsSimpleTest()
		{
			var serverUrl = "http://myServer.com:1234";
			var agent = new ApmAgent(new TestAgentComponents(config: new MockConfigSnapshot(serverUrls: serverUrl)));
			agent.ConfigurationReader.ServerUrls[0].OriginalString.Should().Be(serverUrl);
			var rootedUrl = serverUrl + "/";
			rootedUrl.Should().BeEquivalentTo(agent.ConfigurationReader.ServerUrls[0].AbsoluteUri);
		}

		[Fact]
		public void ServerUrlsInvalidUrlTest()
		{
			var serverUrl = "InvalidUrl";
			var agent = new ApmAgent(new TestAgentComponents(config: new MockConfigSnapshot(serverUrls: serverUrl)));
			agent.ConfigurationReader.ServerUrls[0].Should().Be(DefaultValues.ServerUri);
		}

		[Fact]
		public void ServerUrlInvalidUrlLogTest()
		{
			var serverUrl = "InvalidUrl";
			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentComponents(logger, new MockConfigSnapshot(logger, serverUrls: serverUrl)));
			agent.ConfigurationReader.ServerUrls[0].Should().Be(DefaultValues.ServerUri);

			logger.Lines.Should().NotBeEmpty();
			logger.Lines[0]
				.Should()
				.ContainAll(
					nameof(MockConfigSnapshot),
					"Failed parsing server URL from",
					MockConfigSnapshot.Origin,
					EnvVarNames.ServerUrls,
					serverUrl
				);
		}

		/// <summary>
		/// Makes sure that empty string means sanitization is turned off
		/// </summary>
		[Fact]
		public void SanitizeFieldNamesTestWithEmptyString()
		{
			using (var agent =
				new ApmAgent(new TestAgentComponents(
					config: new MockConfigSnapshot(sanitizeFieldNames: ""))))
				agent.ConfigurationReader.SanitizeFieldNames.Should().BeEmpty();
		}

		/// <summary>
		/// Makes sure that in case SanitizeFieldNames is not set, the agent uses the default SanitizeFieldNames
		/// </summary>
		[Fact]
		public void SanitizeFieldNamesTestWithNoValue()
		{
			using (var agent =
				new ApmAgent(new TestAgentComponents(
					config: new MockConfigSnapshot())))
				agent.ConfigurationReader.SanitizeFieldNames.Should().BeEquivalentTo(DefaultValues.SanitizeFieldNames);
		}

		/// <summary>
		/// Sets 2 servers and makes sure that they are all parsed
		/// </summary>
		[Fact]
		public void ServerUrlsMultipleUrlsTest()
		{
			var serverUrl1 = "http://myServer1.com:1234";
			var serverUrl2 = "http://myServer2.com:1234";
			var serverUrls = $"{serverUrl1},{serverUrl2}";

			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentComponents(logger,
				new MockConfigSnapshot(logger, serverUrls: serverUrls)));

			var parsedUrls = agent.ConfigurationReader.ServerUrls;
			parsedUrls[0].OriginalString.Should().Be(serverUrl1);
			parsedUrls[0].AbsoluteUri.Should().BeEquivalentTo($"{serverUrl1}/");

			parsedUrls[1].OriginalString.Should().Be(serverUrl2);
			parsedUrls[1].AbsoluteUri.Should().BeEquivalentTo($"{serverUrl2}/");
		}

		/// <summary>
		/// Sets 3 server urls, 2 of them are valid, 1 is invalid
		/// Makes sure that the 2 valid urls are parsed and there is a log line for the invalid server url
		/// </summary>
		[Fact]
		public void ServerUrlsMultipleUrlsWith1InvalidUrlTest()
		{
			var serverUrl1 = "http://myServer1.com:1234";
			var serverUrl2 = "invalidUrl";
			var serverUrl3 = "http://myServer2.com:1234";
			var serverUrls = $"{serverUrl1},{serverUrl2},{serverUrl3}";
			var logger = new TestLogger();
			var agent = new ApmAgent(new TestAgentComponents(logger,
				new MockConfigSnapshot(logger, serverUrls: serverUrls)));

			var parsedUrls = agent.ConfigurationReader.ServerUrls;
			parsedUrls.Should().NotBeEmpty().And.HaveCount(2, "seeded 3 but one was invalid");
			parsedUrls[0].OriginalString.Should().Be(serverUrl1);
			parsedUrls[0].AbsoluteUri.Should().BeEquivalentTo($"{serverUrl1}/");

			parsedUrls[1].OriginalString.Should().Be(serverUrl3);
			parsedUrls[1].AbsoluteUri.Should().BeEquivalentTo($"{serverUrl3}/");

			logger.Lines.Should().NotBeEmpty();
			logger.Lines[0]
				.Should()
				.ContainAll(
					nameof(MockConfigSnapshot),
					"Failed parsing server URL from",
					MockConfigSnapshot.Origin,
					EnvVarNames.ServerUrls,
					serverUrl2
				);
		}

		/// <summary>
		/// Makes sure empty spaces are trimmed at the end of the config
		/// </summary>
		[Fact]
		public void ReadServerUrlsWithSpaceAtTheEndViaEnvironmentVariable()
		{
			var serverUrlsWithSpace = "http://myServer:1234 \r\n";
			Environment.SetEnvironmentVariable(EnvVarNames.ServerUrls, serverUrlsWithSpace);
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: new EnvironmentConfigurationReader())))
			{
#if !NETCOREAPP3_0 && !NETCOREAPP3_1
				agent.ConfigurationReader.ServerUrls.First().Should().NotBe(serverUrlsWithSpace);
#endif
				agent.ConfigurationReader.ServerUrls.First().Should().Be("http://myServer:1234");
			}
		}

		[Fact]
		public void SecretTokenSimpleTest()
		{
			var secretToken = "secretToken";
			var agent = new ApmAgent(new TestAgentComponents(config: new MockConfigSnapshot(secretToken: secretToken)));
			agent.ConfigurationReader.SecretToken.Should().Be(secretToken);
		}

		[Fact]
		public void ApiKeySimpleTest()
		{
			var apiKey = "apiKey";
			var agent = new ApmAgent(new TestAgentComponents(config: new MockConfigSnapshot(apiKey: apiKey)));
			agent.ConfigurationReader.ApiKey.Should().Be(apiKey);
		}

		[Fact]
		public void DefaultCaptureHeadersTest()
		{
			using (var agent = new ApmAgent(new TestAgentComponents())) agent.ConfigurationReader.CaptureHeaders.Should().Be(true);
		}

		[Fact]
		public void CaptureBodyConfigTest()
		{
			BuildAgentAndVerify(SupportedValues.CaptureBodyOff);
			BuildAgentAndVerify(SupportedValues.CaptureBodyAll);
			BuildAgentAndVerify(SupportedValues.CaptureBodyErrors);
			BuildAgentAndVerify(SupportedValues.CaptureBodyTransactions);

			void BuildAgentAndVerify(string captureBody)
			{
				using (var agent = new ApmAgent(new TestAgentComponents(config: new MockConfigSnapshot(captureBody: captureBody))))
					agent.ConfigurationReader.CaptureBody.Should().Be(captureBody);
			}
		}

		[Fact]
		public void CaptureBodyContentTypesConfigTest()
		{
			// ReSharper disable once RedundantArgumentDefaultValue
			using (var agent = new ApmAgent(new TestAgentComponents(
				config: new MockConfigSnapshot(captureBodyContentTypes: DefaultValues.CaptureBodyContentTypes))))
			{
				var expected = new List<string> { "application/x-www-form-urlencoded*", "text/*", "application/json*", "application/xml*" };
				agent.ConfigurationReader.CaptureBodyContentTypes.Should().HaveCount(4);
				agent.ConfigurationReader.CaptureBodyContentTypes.Should().BeEquivalentTo(expected);
			}
		}

		[Fact]
		public void SetCaptureHeadersTest()
		{
			Environment.SetEnvironmentVariable(EnvVarNames.CaptureHeaders, "false");
			var config = new EnvironmentConfigurationReader();
			config.CaptureHeaders.Should().Be(false);
		}

		[Fact]
		public void SetCaptureBodyTest()
		{
			//Possible values : "off", "all", "errors", "transactions"
			foreach (var value in SupportedValues.CaptureBodySupportedValues)
			{
				Environment.SetEnvironmentVariable(EnvVarNames.CaptureBody, value);
				var config = new EnvironmentConfigurationReader();
				config.CaptureBody.Should().Be(value);
			}
		}

		[Fact]
		public void SetCaptureBodyContentTypesTest()
		{
			//

			var contentType = "application/x-www-form-urlencoded*";
			Environment.SetEnvironmentVariable(EnvVarNames.CaptureBodyContentTypes, contentType);
			var config = new EnvironmentConfigurationReader();
			config.CaptureBodyContentTypes.Should().HaveCount(1);
			config.CaptureBodyContentTypes[0].Should().Be(contentType);

			Environment.SetEnvironmentVariable(EnvVarNames.CaptureBodyContentTypes,
				"application/x-www-form-urlencoded*, text/*, application/json*, application/xml*");
			config = new EnvironmentConfigurationReader();
			config.CaptureBodyContentTypes.Should().HaveCount(4);
			config.CaptureBodyContentTypes[0].Should().Be("application/x-www-form-urlencoded*");
			config.CaptureBodyContentTypes[1].Should().Be("text/*");
			config.CaptureBodyContentTypes[2].Should().Be("application/json*");
			config.CaptureBodyContentTypes[3].Should().Be("application/xml*");
		}

		[Fact]
		public void DefaultTransactionSampleRateTest()
		{
			using (var agent = new ApmAgent(new TestAgentComponents()))
				agent.ConfigurationReader.TransactionSampleRate.Should().Be(DefaultValues.TransactionSampleRate);
		}


		[Fact]
		public void SetTransactionSampleRateTest()
		{
			Environment.SetEnvironmentVariable(EnvVarNames.TransactionSampleRate, "0.789");
			var config = new EnvironmentConfigurationReader();
			config.TransactionSampleRate.Should().Be(0.789);
		}

		[Fact]
		public void TransactionSampleRateExpectsDotForFloatingPoint()
		{
			Environment.SetEnvironmentVariable(EnvVarNames.TransactionSampleRate, "0,789");
			var config = new EnvironmentConfigurationReader();
			// Since comma was used instead of dot then default value will be used
			config.TransactionSampleRate.Should().Be(DefaultValues.TransactionSampleRate);
		}

		[Fact]
		public void DefaultTransactionMaxSpansTest()
		{
			var reader = new EnvironmentConfigurationReader();
			reader.TransactionMaxSpans.Should().Be(DefaultValues.TransactionMaxSpans);
		}

		[Theory]
		[ClassData(typeof(TransactionMaxSpansTestData))]
		public void TransactionMaxSpansTest(string configurationValue, int expectedValue)
		{
			Environment.SetEnvironmentVariable(EnvVarNames.TransactionMaxSpans, configurationValue);
			var reader = new EnvironmentConfigurationReader();
			reader.TransactionMaxSpans.Should().Be(expectedValue);
		}

		[Fact]
		public void DefaultLogLevelTest() => new ApmAgent(new TestAgentComponents()).ConfigurationReader.LogLevel.Should().Be(LogLevel.Error);

		[Theory]
		[InlineData("Trace", LogLevel.Trace)]
		[InlineData("Debug", LogLevel.Debug)]
		[InlineData("Information", LogLevel.Information)]
		[InlineData("Warning", LogLevel.Warning)]
		[InlineData("Error", LogLevel.Error)]
		[InlineData("Critical", LogLevel.Critical)]
		public void SetLogLevelTest(string logLevelAsString, LogLevel logLevel)
		{
			var logger = new TestLogger(logLevel);
			var agent = new ApmAgent(new TestAgentComponents(logger, new MockConfigSnapshot(logger, logLevelAsString)));
			agent.ConfigurationReader.LogLevel.Should().Be(logLevel);
			agent.Logger.Should().Be(logger);
			foreach (LogLevel enumValue in Enum.GetValues(typeof(LogLevel)))
			{
				if (logLevel <= enumValue)
					agent.Logger.IsEnabled(enumValue).Should().BeTrue();
				else
					agent.Logger.IsEnabled(enumValue).Should().BeFalse();
			}
		}

		[Fact]
		public void SetInvalidLogLevelTest()
		{
			var logger = new TestLogger(LogLevel.Error);
			var logLevelAsString = "InvalidLogLevel";
			var agent = new ApmAgent(new TestAgentComponents(logger, new MockConfigSnapshot(logger, logLevelAsString)));

			agent.ConfigurationReader.LogLevel.Should().Be(LogLevel.Error);
			logger.Lines.Should().NotBeEmpty();
			logger.Lines[0]
				.Should()
				.ContainAll(
					nameof(MockConfigSnapshot),
					"Failed parsing log level from",
					MockConfigSnapshot.Origin,
					EnvVarNames.LogLevel,
					"Defaulting to "
				);
		}

		/// <summary>
		/// The server doesn't accept services with '.' in it.
		/// This test makes sure we don't have '.' in the default service name.
		/// </summary>
		[Fact]
		public void DefaultServiceNameTest()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("TestTransactionName", "TestTransactionType", t => { Thread.Sleep(2); });

				//By default XUnit uses 'testhost' as the entry assembly, and that is what the
				//agent reports if we don't set it to anything:
				var serviceName = agent.Service.Name;
				serviceName.Should().NotBeNullOrWhiteSpace();
				serviceName.Should().NotContain(".");
			}
		}

		/// <summary>
		/// Sets the ELASTIC_APM_SERVICE_NAME environment variable and makes sure that
		/// when the agent sends data to the server it has the value from the
		/// ELASTIC_APM_SERVICE_NAME environment variable as service name.
		/// </summary>
		[Fact]
		public void ReadServiceNameViaEnvironmentVariable()
		{
			var serviceName = "MyService123";
			Environment.SetEnvironmentVariable(EnvVarNames.ServiceName, serviceName);
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: new EnvironmentConfigurationReader())))
			{
				agent.Tracer.CaptureTransaction("TestTransactionName", "TestTransactionType", t => { Thread.Sleep(2); });

				agent.Service.Name.Should().Be(serviceName);
			}
		}

		/// <summary>
		/// Sets the ELASTIC_APM_SERVICE_NAME environment variable to a value that contains a '.'
		/// Makes sure that when the agent sends data to the server it has the value from the
		/// ELASTIC_APM_SERVICE_NAME environment variable as service name and also makes sure that
		/// the '.' is replaced.
		/// </summary>
		[Fact]
		public void ReadServiceNameWithDotViaEnvironmentVariable()
		{
			var serviceName = "My.Service.Test";
			Environment.SetEnvironmentVariable(EnvVarNames.ServiceName, serviceName);
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: new EnvironmentConfigurationReader())))
			{
				agent.Tracer.CaptureTransaction("TestTransactionName", "TestTransactionType", t => { Thread.Sleep(2); });

				agent.Service.Name.Should().Be(serviceName.Replace('.', '_'));
				agent.Service.Name.Should().NotContain(".");
			}
		}

		/// <summary>
		/// The test makes sure we validate service name.
		/// </summary>
		[Fact]
		public void ReadInvalidServiceNameViaEnvironmentVariable()
		{
			var serviceName = "MyService123!";
			Environment.SetEnvironmentVariable(EnvVarNames.ServiceName, serviceName);
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: new EnvironmentConfigurationReader())))
			{
				agent.Tracer.CaptureTransaction("TestTransactionName", "TestTransactionType", t => { Thread.Sleep(2); });

				agent.Service.Name.Should().NotBe(serviceName);
				agent.Service.Name.Should()
					.MatchRegex("^[a-zA-Z0-9 _-]+$")
					.And.Be("MyService123_");
			}
		}

		/// <summary>
		/// The test makes sure that unknown service name value fits to all constraints.
		/// </summary>
		[Fact]
		public void UnknownServiceNameValueTest()
		{
			var serviceName = DefaultValues.UnknownServiceName;
			Environment.SetEnvironmentVariable(EnvVarNames.ServiceName, serviceName);
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: new EnvironmentConfigurationReader())))
			{
				agent.Tracer.CaptureTransaction("TestTransactionName", "TestTransactionType", t => { Thread.Sleep(2); });

				agent.Service.Name.Should().Be(serviceName);
				agent.Service.Name.Should().MatchRegex("^[a-zA-Z0-9 _-]+$");
			}
		}

		/// <summary>
		/// Sets the ELASTIC_APM_SERVICE_VERSION environment variable and makes sure that
		/// when the agent sends data to the server it has the value from the
		/// ELASTIC_APM_SERVICE_VERSION environment variable as service version.
		/// </summary>
		[Fact]
		public void ReadServiceVersionViaEnvironmentVariable()
		{
			var serviceVersion = "2.1.0.5";
			Environment.SetEnvironmentVariable(EnvVarNames.ServiceVersion, serviceVersion);
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: new EnvironmentConfigurationReader())))
			{
				agent.Tracer.CaptureTransaction("TestTransactionName", "TestTransactionType", t => { Thread.Sleep(2); });

				agent.Service.Version.Should().Be(serviceVersion);
			}
		}

		[Fact]
		public void ReadServiceNodeNameViaEnvironmentVariable()
		{
			// Arrange
			var serviceNodeName = "Some service node name";
			Environment.SetEnvironmentVariable(EnvVarNames.ServiceNodeName, serviceNodeName);
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender, config: new EnvironmentConfigurationReader())))
			{
				// Act
				agent.Tracer.CaptureTransaction("TestTransactionName", "TestTransactionType", t => { Thread.Sleep(2); });

				// Assert
				agent.Service.Node.ConfiguredName.Should().Be(serviceNodeName);
			}
		}

		/// <summary>
		/// In case the user does not provide us a service name we try to calculate it based on the callstack.
		/// This test makes sure we recognize mscorlib and our own assemblies correctly in the
		/// <see cref="AbstractConfigurationReader.IsMsOrElastic(byte[])" /> method.
		/// </summary>
		[Fact]
		public void TestAbstractConfigurationReaderIsMsOrElastic()
		{
			var elasticToken = new byte[] { 174, 116, 0, 210, 193, 137, 207, 34 };
			var mscorlibToken = new byte[] { 183, 122, 92, 86, 25, 52, 224, 137 };

			AbstractConfigurationReader.IsMsOrElastic(elasticToken).Should().BeTrue();

			AbstractConfigurationReader.IsMsOrElastic(new byte[] { 0 }).Should().BeFalse();
			AbstractConfigurationReader.IsMsOrElastic(new byte[] { }).Should().BeFalse();

			AbstractConfigurationReader
				.IsMsOrElastic(new[]
				{
					elasticToken[0], mscorlibToken[1], elasticToken[2], mscorlibToken[3], elasticToken[4], mscorlibToken[5], elasticToken[6],
					mscorlibToken[7]
				})
				.Should()
				.BeFalse();
		}

		/// <summary>
		/// Makes sure that the <see cref="EnvironmentConfigurationReader" /> logs
		/// in case it reads an invalid URL.
		/// </summary>
		[Fact]
		public void LoggerNotNull()
		{
			Environment.SetEnvironmentVariable(EnvVarNames.ServerUrls, "localhost"); //invalid, it should be "http://localhost"
			var testLogger = new TestLogger();
			var config = new EnvironmentConfigurationReader(testLogger);
			var serverUrl = config.ServerUrls.FirstOrDefault();

			serverUrl.Should().NotBeNull();
			testLogger.Lines.Should().NotBeEmpty();
		}

		[Fact]
		public void SetMetricsIntervalTo10S()
			=> MetricsIntervalTestCommon("10s").Should().Be(10 * 1000);

		/// <summary>
		/// Sets the metrics interval to '500ms'
		/// Makes sure that 500ms defaults to 0, since the minimum is 1s
		/// </summary>
		[Fact]
		public void SetMetricsIntervalTo500Ms()
			=> MetricsIntervalTestCommon("500ms").Should().Be(0);

		[Fact]
		public void SetMetricsIntervalTo1500Ms()
			=> MetricsIntervalTestCommon("1500ms").Should().Be(1500);

		[Fact]
		public void SetMetricsIntervalTo1HourAs60Minutes()
			=> MetricsIntervalTestCommon("60m").Should().Be(60 * 60 * 1000);

		[Fact]
		public void SetMetricsIntervalTo1HourUsingUnsupportedUnits()
			=> MetricsIntervalTestCommon("1h").Should().Be(DefaultValues.MetricsIntervalInMilliseconds);

		[Fact]
		public void SetMetricsIntervalTo1M()
			=> MetricsIntervalTestCommon("1m").Should().Be(60 * 1000);

		/// <summary>
		/// Sets the metrics interval to '10'.
		/// Makes sure that '10' defaults to '10s'
		/// </summary>
		[Fact]
		public void SetMetricsIntervalTo10()
			=> MetricsIntervalTestCommon("10").Should().Be(10 * 1000);

		/// <summary>
		/// Any negative value should be treated as 0
		/// </summary>
		[Fact]
		public void SetMetricsIntervalToNegativeNoUnits()
			=> MetricsIntervalTestCommon("-1").Should().Be(0);

		[Fact]
		public void SetMetricsIntervalToNegativeSeconds()
			=> MetricsIntervalTestCommon("-0.3s").Should().Be(0);

		[Fact]
		public void SetMetricsIntervalToNegativeMinutes()
			=> MetricsIntervalTestCommon("-5m").Should().Be(0);

		[Fact]
		public void SetMetricsIntervalToNegativeMilliseconds()
			=> MetricsIntervalTestCommon("-5ms").Should().Be(0);

		[Fact]
		public void SetSpanFramesMinDurationAndStackTraceLimit()
		{
			Environment.SetEnvironmentVariable(EnvVarNames.SpanFramesMinDuration, DefaultValues.SpanFramesMinDuration);
			Environment.SetEnvironmentVariable(EnvVarNames.StackTraceLimit, DefaultValues.StackTraceLimit.ToString());
			var config = new EnvironmentConfigurationReader(new NoopLogger());
			config.SpanFramesMinDurationInMilliseconds.Should().Be(DefaultValues.SpanFramesMinDurationInMilliseconds);
			config.StackTraceLimit.Should().Be(DefaultValues.StackTraceLimit);
		}

		/// <summary>
		/// Make sure <see cref="DefaultValues.MetricsInterval" /> and <see cref="DefaultValues.MetricsIntervalInMilliseconds" />
		/// are in sync
		/// </summary>
		[Fact]
		public void MetricsIntervalDefaultValuesInSync()
			=> MetricsIntervalTestCommon(DefaultValues.MetricsInterval).Should().Be(DefaultValues.MetricsIntervalInMilliseconds);

		[Fact]
		public void SpanFramesMinDurationDefaultValuesInSync()
		{
			Environment.SetEnvironmentVariable(EnvVarNames.SpanFramesMinDuration, DefaultValues.SpanFramesMinDuration);
			var config = new EnvironmentConfigurationReader(new NoopLogger());
			config.SpanFramesMinDurationInMilliseconds.Should().Be(DefaultValues.SpanFramesMinDurationInMilliseconds);
		}

		[InlineData("2", 2)]
		[InlineData("0", 0)]
		[InlineData("-2", -2)]
		[InlineData("2147483647", int.MaxValue)]
		[InlineData("-2147483648", int.MinValue)]
		[InlineData("2.32", DefaultValues.StackTraceLimit)]
		[InlineData("2,32", DefaultValues.StackTraceLimit)]
		// ReSharper disable once StringLiteralTypo
		[InlineData("asdf", DefaultValues.StackTraceLimit)]
		[Theory]
		public void StackTraceLimit(string configValue, int expectedValue)
		{
			using (var agent =
				new ApmAgent(new TestAgentComponents(config: new MockConfigSnapshot(stackTraceLimit: configValue))))
				agent.ConfigurationReader.StackTraceLimit.Should().Be(expectedValue);
		}

		[InlineData("2ms", 2)]
		[InlineData("2s", 2 * 1000)]
		[InlineData("2m", 2 * 60 * 1000)]
		[InlineData("2", 2)]
		[InlineData("-2ms", -2)]
		// ReSharper disable once StringLiteralTypo
		[InlineData("dsfkldfs", DefaultValues.SpanFramesMinDurationInMilliseconds)]
		[InlineData("2,32", DefaultValues.SpanFramesMinDurationInMilliseconds)]
		[Theory]
		public void SpanFramesMinDurationInMilliseconds(string configValue, int expectedValue)
		{
			using (var agent =
				new ApmAgent(new TestAgentComponents(
					config: new MockConfigSnapshot(spanFramesMinDurationInMilliseconds: configValue))))
				agent.ConfigurationReader.SpanFramesMinDurationInMilliseconds.Should().Be(expectedValue);
		}

		[InlineData("123ms", 123)]
		[InlineData("976s", 976 * 1000)]
		[InlineData("2m", 2 * 60 * 1000)]
		[InlineData("567", 567 * 1000)]
		[InlineData("0", 0)]
		[InlineData("0s", 0)]
		[InlineData("0ms", 0)]
		[InlineData("-3ms", DefaultValues.FlushIntervalInMilliseconds)]
		[InlineData("-1", DefaultValues.FlushIntervalInMilliseconds)]
		// ReSharper disable once StringLiteralTypo
		[InlineData("dsfkldfs", DefaultValues.FlushIntervalInMilliseconds)]
		[InlineData("2,32", DefaultValues.FlushIntervalInMilliseconds)]
		[InlineData("785zz", DefaultValues.FlushIntervalInMilliseconds)]
		[Theory]
		public void FlushInterval_tests(string configValue, int expectedValueInMilliseconds)
		{
			using (var agent =
				new ApmAgent(new TestAgentComponents(
					config: new MockConfigSnapshot(flushInterval: configValue))))
				agent.ConfigurationReader.FlushInterval.Should().Be(TimeSpan.FromMilliseconds(expectedValueInMilliseconds));
		}

		[InlineData("1", 1)]
		[InlineData("23", 23)]
		[InlineData("654", 654)]
		[InlineData("0", DefaultValues.MaxQueueEventCount)]
		[InlineData("-1", DefaultValues.MaxQueueEventCount)]
		[InlineData("-23", DefaultValues.MaxQueueEventCount)]
		[InlineData("-654", DefaultValues.MaxQueueEventCount)]
		// ReSharper disable once StringLiteralTypo
		[InlineData("0aefjw", DefaultValues.MaxQueueEventCount)]
		// ReSharper disable once StringLiteralTypo
		[InlineData("aefjw9", DefaultValues.MaxQueueEventCount)]
		[InlineData("2,32", DefaultValues.MaxQueueEventCount)]
		[InlineData("2.32", DefaultValues.MaxQueueEventCount)]
		[Theory]
		public void MaxQueueEventCount_tests(string configValue, int expectedValue)
		{
			using (var agent =
				new ApmAgent(new TestAgentComponents(
					config: new MockConfigSnapshot(maxQueueEventCount: configValue))))
				agent.ConfigurationReader.MaxQueueEventCount.Should().Be(expectedValue);
		}

		[InlineData("1", 1)]
		[InlineData("23", 23)]
		[InlineData("654", 654)]
		[InlineData("0", DefaultValues.MaxBatchEventCount)]
		[InlineData("-1", DefaultValues.MaxBatchEventCount)]
		[InlineData("-23", DefaultValues.MaxBatchEventCount)]
		[InlineData("-654", DefaultValues.MaxBatchEventCount)]
		// ReSharper disable once StringLiteralTypo
		[InlineData("0aefjw", DefaultValues.MaxBatchEventCount)]
		// ReSharper disable once StringLiteralTypo
		[InlineData("aefjw9", DefaultValues.MaxBatchEventCount)]
		[InlineData("2,32", DefaultValues.MaxBatchEventCount)]
		[InlineData("2.32", DefaultValues.MaxBatchEventCount)]
		[Theory]
		public void MaxBatchEventCount_tests(string configValue, int expectedValue)
		{
			using (var agent =
				new ApmAgent(new TestAgentComponents(
					config: new MockConfigSnapshot(maxBatchEventCount: configValue))))
				agent.ConfigurationReader.MaxBatchEventCount.Should().Be(expectedValue);
		}

		[InlineData("false", false)]
		[InlineData("true", true)]
		[InlineData("fALSe", false)]
		[InlineData("TRUE", true)]
		[InlineData("", DefaultValues.CentralConfig)]
		[InlineData(null, DefaultValues.CentralConfig)]
		// ReSharper disable once StringLiteralTypo
		[InlineData("aefjw9", DefaultValues.CentralConfig)]
		[Theory]
		public void CentralConfig_tests(string configValue, bool expectedValue)
		{
			using (var agent =
				new ApmAgent(new TestAgentComponents(
					config: new MockConfigSnapshot(centralConfig: configValue))))
				agent.ConfigurationReader.CentralConfig.Should().Be(expectedValue);
		}

		[Theory]
		[MemberData(nameof(GlobalLabelsValidVariantsToTest))]
		public void GlobalLabels_valid_input_tests(string optionValue, IReadOnlyDictionary<string, string> expectedParseResult)
		{
			using (var agent = new ApmAgent(new TestAgentComponents(config: new MockConfigSnapshot(globalLabels: optionValue))))
				agent.ConfigurationReader.GlobalLabels.Should().Equal(expectedParseResult);
		}

		[Theory]
		[InlineData("key")] // no key value separator
		[InlineData(",k=v")] // no key value separator in the first pair
		[InlineData("k=v,")] // no key value separator in the last pair
		[InlineData("key1=value1,,key3=value3")] // no key value separator in the middle pair
		[InlineData("key=value1,key2=value2,key=value3")] // more than one pair with the same key
		[InlineData("=,=")] // two pairs with the same key (empty string)
		public void GlobalLabels_invalid_input_tests(string optionValue)
		{
			var mockLogger = new TestLogger();
			using (var agent = new ApmAgent(new TestAgentComponents(mockLogger
				, new MockConfigSnapshot(mockLogger, globalLabels: optionValue))))
				agent.ConfigurationReader.GlobalLabels.Should().BeEmpty();
			mockLogger.Lines.Should()
				.Contain(line =>
					line.ContainsOrdinalIgnoreCase("Error")
					&& line.ContainsOrdinalIgnoreCase(nameof(AbstractConfigurationReader))
					&& line.ContainsOrdinalIgnoreCase("GlobalLabels")
				);
		}

		[Theory]
		[InlineData("My HostName", "My HostName")]
		[InlineData("", null)]
		[InlineData(null, null)]
		public void Set_HostName(string hostName, string expected)
		{
			var agent = new ApmAgent(new TestAgentComponents(config: new MockConfigSnapshot(hostName: hostName)));
			agent.ConfigurationReader.HostName.Should().Be(expected);
		}


		/// <summary>
		/// Disables CPU metrics and makes sure that remaining metrics are still captured
		/// </summary>
		[Fact]
		public void DisableMetrics_DisableCpuMetrics()
		{
			var mockPayloadSender = new MockPayloadSender();
			Environment.SetEnvironmentVariable(EnvVarNames.DisableMetrics, "*cpu*");

			using var metricsProvider = new MetricsCollector(new NoopLogger(), mockPayloadSender, new EnvironmentConfigurationReader());
			metricsProvider.CollectAllMetrics();

			mockPayloadSender.Metrics.Should().NotBeEmpty();

			var firstMetrics = mockPayloadSender.Metrics.First();
			firstMetrics.Should().NotBeNull();

			firstMetrics.Samples.Should().NotContain(n => n.KeyValue.Key.Contains("cpu"));

			//These are collected on all platforms, with the given config they always should be there
			firstMetrics.Samples.Should()
				.Contain(n => n.KeyValue.Key.Equals("system.process.memory.size", StringComparison.InvariantCultureIgnoreCase));
			firstMetrics.Samples.Should()
				.Contain(n => n.KeyValue.Key.Equals("system.process.memory.rss.bytes", StringComparison.InvariantCultureIgnoreCase));
		}

		[Fact]
		public void DefaultVerifyServerCertIsTrue()
		{
			var agent = new ApmAgent(new TestAgentComponents());
			agent.ConfigurationReader.VerifyServerCert.Should().BeTrue();
		}

		[Theory]
		[InlineData("false", false)]
		[InlineData("true", true)]
		[InlineData("nonsense value", true)]
		public void SetVerifyServerCert(string verifyServerCert, bool expected)
		{
			var agent = new ApmAgent(new TestAgentComponents(config: new MockConfigSnapshot(verifyServerCert: verifyServerCert)));
			agent.ConfigurationReader.VerifyServerCert.Should().Be(expected);
		}

		/// <summary>
		/// Sets the TransactionIgnoreUrls setting and makes sure the agent config contains those
		/// </summary>
		[Fact]
		public void TransactionIgnoreUrlsTestWithCustomSettingNoSpace()
		{
			var agent = new ApmAgent(new TestAgentComponents(config: new MockConfigSnapshot(transactionIgnoreUrls: "*index,*myPageToIgnore*")));
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "INDEX").Should().BeTrue();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "/home/INDEX").Should().BeTrue();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "INdEX").Should().BeTrue();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "index").Should().BeTrue();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "myPageToIgnore").Should().BeTrue();

			// default list is not applied when TransactionIgnoreUrls is set
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "myJsScripts.js").Should().BeFalse();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "bootstrap.css").Should().BeFalse();
		}

		/// <summary>
		/// Sets the TransactionIgnoreUrls setting and makes sure the agent config contains those.
		/// It uses a space in the list.
		/// </summary>
		[Fact]
		public void TransactionIgnoreUrlsTestWithCustomSettingWithSpace()
		{
			var agent = new ApmAgent(new TestAgentComponents(config: new MockConfigSnapshot(transactionIgnoreUrls: "*index, *myPageToIgnore*")));
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "INDEX").Should().BeTrue();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "/home/INDEX").Should().BeTrue();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "INdEX").Should().BeTrue();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "index").Should().BeTrue();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "myPageToIgnore").Should().BeTrue();

			// default list is not applied when TransactionIgnoreUrls is set
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "myJsScripts.js").Should().BeFalse();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "bootstrap.css").Should().BeFalse();
		}

		/// <summary>
		/// Tests case sensitive TransactionIgnoreUrls
		/// </summary>
		[Fact]
		public void TransactionIgnoreUrlsTestWithCustomCaseSensitiveSetting()
		{
			var agent = new ApmAgent(
				new TestAgentComponents(config: new MockConfigSnapshot(transactionIgnoreUrls: "(?-i)*index,(?-i)*myPageToIgnore*")));
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "index").Should().BeTrue();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "INDEX").Should().BeFalse();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "/home/INDEX").Should().BeFalse();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "INdEX").Should().BeFalse();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "myPageToIgnore").Should().BeTrue();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "myPagetoignore").Should().BeFalse();

			// default list is not applied when TransactionIgnoreUrls is set
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "myJsScripts.js").Should().BeFalse();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "bootstrap.css").Should().BeFalse();
		}

		/// <summary>
		/// Applies the default agent settings and makes sure that *js and *css are filtered out, but
		/// some other random urls are not ignored.
		/// </summary>
		[Fact]
		public void TransactionIgnoreUrlsTestWithDefaultSetting()
		{
			// use default settings
			var agent = new ApmAgent(new TestAgentComponents(config: new MockConfigSnapshot()));

			// some random pages should be captured
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "INDEX").Should().BeFalse();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "/home/INDEX").Should().BeFalse();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "INdEX").Should().BeFalse();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "index").Should().BeFalse();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "myRandomPage").Should().BeFalse();

			// *js and *css is by default ignored
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "myJsScripts.js").Should().BeTrue();
			WildcardMatcher.IsAnyMatch(agent.ConfigurationReader.TransactionIgnoreUrls, "bootstrap.css").Should().BeTrue();
		}

		private static double MetricsIntervalTestCommon(string configValue)
		{
			Environment.SetEnvironmentVariable(EnvVarNames.MetricsInterval, configValue);
			var testLogger = new TestLogger();
			var config = new EnvironmentConfigurationReader(testLogger);
			return config.MetricsIntervalInMilliseconds;
		}

		public void Dispose()
		{
			Environment.SetEnvironmentVariable(EnvVarNames.ServerUrls, null);
			Environment.SetEnvironmentVariable(EnvVarNames.MetricsInterval, null);
		}
	}
}
