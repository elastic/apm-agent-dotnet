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

namespace Elastic.Apm.Tests.BackendCommTests.CentralConfig
{
	public class CentralConfigResponseParserTests
	{
		private readonly CentralConfigResponseParser _parser;
		private readonly HttpResponseMessage _correctResponse;

		public CentralConfigResponseParserTests()
		{
			_parser = new CentralConfigResponseParser(new NoopLogger());
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
			var parser = new CentralConfigResponseParser(testLogger);

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
						$"{{\"{CentralConfigResponseParser.CentralConfigPayload.CaptureBodyKey}\": \"{value}\"}}",
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
					$"{{\"{CentralConfigResponseParser.CentralConfigPayload.TransactionMaxSpansKey}\": \"{100}\"}}",
					new Action<CentralConfigurationReader>(cfg =>
					{
						cfg.TransactionMaxSpans.Should()
							.NotBeNull()
							.And.Be(100);
					})
				};

				yield return new object[]
				{
					$"{{\"{CentralConfigResponseParser.CentralConfigPayload.TransactionSampleRateKey}\": \"{0.75}\"}}",
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
					$"{{\"{CentralConfigResponseParser.CentralConfigPayload.CaptureBodyContentTypesKey}\": \"{captureBodyContentTypes}\"}}",
					new Action<CentralConfigurationReader>(cfg =>
					{
						cfg.CaptureBodyContentTypes.Should()
							.NotBeNull()
							.And.BeEquivalentTo(captureBodyContentTypes.Split(',').Select(x => x.Trim()));
					})
				};

				yield return new object[]
				{
					$"{{\"{CentralConfigResponseParser.CentralConfigPayload.StackTraceLimitKey}\": \"{150}\"}}",
					new Action<CentralConfigurationReader>(cfg =>
					{
						cfg.StackTraceLimit.Should()
							.NotBeNull()
							.And.Be(150);
					})
				};

				yield return new object[]
				{
					$"{{\"{CentralConfigResponseParser.CentralConfigPayload.SpanFramesMinDurationKey}\": \"{150}\"}}",
					new Action<CentralConfigurationReader>(cfg =>
					{
						cfg.SpanFramesMinDurationInMilliseconds.Should()
							.NotBeNull()
							.And.Be(150);
					})
				};

				yield return new object[]
				{
					$"{{\"{CentralConfigResponseParser.CentralConfigPayload.CaptureHeadersKey}\": \"{false}\"}}",
					new Action<CentralConfigurationReader>(cfg =>
					{
						cfg.CaptureHeaders.Should()
							.NotBeNull()
							.And.Be(false);
					})
				};

				yield return new object[]
				{
					$"{{\"{CentralConfigResponseParser.CentralConfigPayload.CaptureHeadersKey}\": \"{true}\"}}",
					new Action<CentralConfigurationReader>(cfg =>
					{
						cfg.CaptureHeaders.Should()
							.NotBeNull()
							.And.Be(true);
					})
				};

				foreach (var value in Enum.GetValues(typeof(LogLevel)))
				{
					yield return new object[]
					{
						$"{{\"{CentralConfigResponseParser.CentralConfigPayload.LogLevelKey}\": \"{value}\"}}",
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
