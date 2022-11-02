// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Elastic.Apm.BackendComm.CentralConfig;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

// Disable obsolete-warning due to Configuration.SpanFramesMinDurationInMilliseconds access.
#pragma warning disable CS0618

namespace Elastic.Apm.Tests.BackendCommTests.CentralConfig
{
	public class CentralConfigResponseParserTests
	{
		private readonly CentralConfigurationResponseParser _parser;
		private readonly HttpResponseMessage _correctResponse;

		public CentralConfigResponseParserTests()
		{
			_parser = new CentralConfigurationResponseParser(new NoopLogger());
			_correctResponse = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Headers = { ETag = new EntityTagHeaderValue("\"33a64df551425fcc55e4d42a148795d9f25f89d4\"") }
			};
		}

		[Fact]
		public void ParseHttpResponse_ShouldUseMaxAgeHeader()
		{
			// Arrange
			var response = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.NotModified,
				Headers = { CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromMinutes(5) } }
			};

			// Act
			var (_, waitInfoS) = _parser.ParseHttpResponse(response, string.Empty);

			// Assert
			waitInfoS.Interval.Should().Be(TimeSpan.FromMinutes(5));
		}

		/// <summary>
		/// According to spec, the agent should retry fetching central config with not less than 5 sec interval.
		/// Makes sure that MaxAge headers with less than 5 sec fall back to use a 5 sec wait time.
		/// </summary>
		/// <param name="seconds">The MaxAge header returned to the agent.</param>
		[Theory]
		[InlineData(1)]
		[InlineData(4)]
		[InlineData(5)]
		public void ParseHttpResponse_ShouldUse5SecWaitTime_WhenMaxAgeIsLessThan5Sec(int seconds)
		{
			var response = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.NotModified,
				Headers = { CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromSeconds(seconds) } }
			};

			var (_, waitInfoS) = _parser.ParseHttpResponse(response, string.Empty);

			waitInfoS.Interval.Should().Be(TimeSpan.FromSeconds(5));

			if(seconds < 5)
			{
				waitInfoS.Reason.Should().Be("The max-age directive in Cache-Control header in APM Server's response is less than 5 seconds, "
					+ "which is less than expected by the spec - falling back to use 5 seconds wait time.");
			}
			else
			{
				waitInfoS.Reason.Should().NotBe("The max-age directive in Cache-Control header in APM Server's response is less than 5 seconds, "
					+ "which is less than expected by the spec - falling back to use 5 seconds wait time.");
			}
		}

		/// <summary>
		/// Makes sure the agent handles 0 or negative MaxAge headers by falling back to the default wait time (5 minutes).
		/// </summary>
		/// <param name="seconds">The MaxAge header returned to the agent.</param>
		[Theory]
		[InlineData(0)]
		[InlineData(-1)]
		[InlineData(-10)]
		[InlineData(int.MinValue)]
		public void  ParseHttpResponse_ShouldUseDefaultWaitTime_WhenMaxAgeIsZeroOrNegative(int seconds)
		{
			var response = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.NotModified,
				Headers = { CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromSeconds(seconds) } }
			};

			var (_, waitInfoS) = _parser.ParseHttpResponse(response, string.Empty);

			waitInfoS.Interval.Should().Be(TimeSpan.FromMinutes(5));
			waitInfoS.Reason.Should().Be("The max-age directive in Cache-Control header in APM Server's response is zero or negative, "
				+ $"which is invalid - falling back to use default ({CentralConfigurationResponseParser.WaitTimeIfNoCacheControlMaxAge.Minutes} minutes) wait time.");
		}

		[Fact]
		public void ParseHttpResponse_ShouldReturnNullConfigDelta_WhenResponseStatusCodeIs304()
		{
			// Arrange
			var response = new HttpResponseMessage { StatusCode = HttpStatusCode.NotModified };

			// Act
			var (configDelta, _) = _parser.ParseHttpResponse(response, string.Empty);

			// Assert
			configDelta.Should().BeNull();
		}

		[Theory]
		[InlineData(HttpStatusCode.Moved)]
		[InlineData(HttpStatusCode.BadRequest)]
		[InlineData(HttpStatusCode.Forbidden)]
		[InlineData(HttpStatusCode.NotFound)]
		[InlineData(HttpStatusCode.ServiceUnavailable)]
		public void ParseHttpResponse_ShouldThrowException_WhenStatusCodeIsNotSuccessAndNot304(HttpStatusCode statusCode)
		{
			// Arrange
			var response = new HttpResponseMessage { StatusCode = statusCode };

			// Act + Assert
			var exception = Assert.Throws<CentralConfigurationFetcher.FailedToFetchConfigException>(
				() => _parser.ParseHttpResponse(response, string.Empty));

			exception.Message.Should().Contain("HTTP status code is ");
		}

		[Fact]
		public void ParseHttpResponse_ShouldThrowException_WhenETagHeaderIsMissed()
		{
			// Arrange
			var response = new HttpResponseMessage { StatusCode = HttpStatusCode.OK };

			// Act + Assert
			var exception = Assert.Throws<CentralConfigurationFetcher.FailedToFetchConfigException>(
				() => _parser.ParseHttpResponse(response, string.Empty));

			exception.Message.Should().Contain("doesn't have ETag header");
		}

		[Fact]
		public void ParseHttpResponse_ShouldReturnEmptyConfigDelta_WhenResponseBodyIsEmpty()
		{
			// Arrange + Act
			var (configDelta, _) = _parser.ParseHttpResponse(_correctResponse, "{}");

			// Assert
			configDelta.Should().NotBeNull();
			configDelta.CaptureBody.Should().BeNull();
			configDelta.TransactionMaxSpans.Should().BeNull();
			configDelta.TransactionSampleRate.Should().BeNull();
			configDelta.CaptureBodyContentTypes.Should().BeNull();
			configDelta.CaptureHeaders.Should().BeNull();
			configDelta.LogLevel.Should().BeNull();
			configDelta.SpanFramesMinDurationInMilliseconds.Should().BeNull();
			configDelta.StackTraceLimit.Should().BeNull();
		}

		[Fact]
		public void ParseHttpResponse_ShouldLogUnknownKeys()
		{
			// Arrange
			var testLogger = new TestLogger(LogLevel.Information);
			var parser = new CentralConfigurationResponseParser(testLogger);

			// Act
			parser.ParseHttpResponse(_correctResponse, "{\"unknownKey\": \"value\"}");

			// Assert
			testLogger.Lines.Count.Should().Be(1);
			testLogger.Lines[0]
				.Should()
				.Contain(
					"Central configuration response contains keys that are not in the list of options that can be changed after Agent start: `[unknownKey, value]'");
		}

		public static IEnumerable<object[]> ConfigDeltaData
		{
			get
			{
				foreach (var value in ConfigConsts.SupportedValues.CaptureBodySupportedValues)
				{
					yield return new object[]
					{
						$"{{\"{CentralConfigurationResponseParser.CentralConfigPayload.CaptureBodyKey}\": \"{value}\"}}",
						new Action<CentralConfigurationReader>(cfg =>
						{
							cfg.CaptureBody.Should()
								.NotBeNull()
								.And.Be(value);
						})
					};
				}

				yield return new object[]
				{
					$"{{\"{CentralConfigurationResponseParser.CentralConfigPayload.TransactionMaxSpansKey}\": \"{100}\"}}",
					new Action<CentralConfigurationReader>(cfg =>
					{
						cfg.TransactionMaxSpans.Should()
							.NotBeNull()
							.And.Be(100);
					})
				};

				yield return new object[]
				{
					$"{{\"{CentralConfigurationResponseParser.CentralConfigPayload.TransactionSampleRateKey}\": \"0.75\"}}",
					new Action<CentralConfigurationReader>(cfg =>
					{
						cfg.TransactionSampleRate.Should()
							.NotBeNull()
							.And.Be(0.75);
					})
				};

				var captureBodyContentTypes = "application/x-www-form-urlencoded*, application/json*";
				yield return new object[]
				{
					$"{{\"{CentralConfigurationResponseParser.CentralConfigPayload.CaptureBodyContentTypesKey}\": \"{captureBodyContentTypes}\"}}",
					new Action<CentralConfigurationReader>(cfg =>
					{
						cfg.CaptureBodyContentTypes.Should()
							.NotBeNull()
							.And.BeEquivalentTo(captureBodyContentTypes.Split(',').Select(x => x.Trim()));
					})
				};

				yield return new object[]
				{
					$"{{\"{CentralConfigurationResponseParser.CentralConfigPayload.StackTraceLimitKey}\": \"{150}\"}}",
					new Action<CentralConfigurationReader>(cfg =>
					{
						cfg.StackTraceLimit.Should()
							.NotBeNull()
							.And.Be(150);
					})
				};

				yield return new object[]
				{
					$"{{\"{CentralConfigurationResponseParser.CentralConfigPayload.SpanFramesMinDurationKey}\": \"{150}\"}}",
					new Action<CentralConfigurationReader>(cfg =>
					{
						cfg.SpanFramesMinDurationInMilliseconds.Should()
							.NotBeNull()
							.And.Be(150);
					})
				};

				yield return new object[]
				{
					$"{{\"{CentralConfigurationResponseParser.CentralConfigPayload.CaptureHeadersKey}\": \"{false}\"}}",
					new Action<CentralConfigurationReader>(cfg =>
					{
						cfg.CaptureHeaders.Should()
							.NotBeNull()
							.And.Be(false);
					})
				};

				yield return new object[]
				{
					$"{{\"{CentralConfigurationResponseParser.CentralConfigPayload.CaptureHeadersKey}\": \"{true}\"}}",
					new Action<CentralConfigurationReader>(cfg =>
					{
						cfg.CaptureHeaders.Should()
							.NotBeNull()
							.And.Be(true);
					})
				};

				yield return new object[]
				{
					// making sure empty json does not cause any error.
					"{}", new Action<CentralConfigurationReader>(cfg =>
					{
						cfg.Should().NotBeNull();
					})
				};

				foreach (var value in Enum.GetValues(typeof(LogLevel)))
				{
					yield return new object[]
					{
						$"{{\"{CentralConfigurationResponseParser.CentralConfigPayload.LogLevelKey}\": \"{value}\"}}",
						new Action<CentralConfigurationReader>(cfg =>
						{
							cfg.LogLevel.Should()
								.NotBeNull()
								.And.Be(value);
						})
					};
				}
			}
		}

		[Theory]
		[MemberData(nameof(ConfigDeltaData))]
		internal void ParseHttpResponse_ShouldCorrectlyCalculateConfigDelta(string httpResponseBody, Action<CentralConfigurationReader> assert)
		{
			// Arrange + Act
			var (configDelta, _) = _parser.ParseHttpResponse(_correctResponse, httpResponseBody);

			// Assert
			configDelta.Should().NotBeNull();
			assert.Invoke(configDelta);
		}
	}
}
