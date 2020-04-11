using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Elastic.Apm.BackendComm.CentralConfig;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.BackendCommTests.CentralConfig
{
	public class CentralConfigResponseParserTests
	{
		private readonly CentralConfigResponseParser _parser;

		public CentralConfigResponseParserTests()
		{
			_parser = new CentralConfigResponseParser(new NoopLogger());
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
			Assert.Throws<CentralConfigFetcher.FailedToFetchConfigException>(
				() => _parser.ParseHttpResponse(response, string.Empty));
		}

		[Fact]
		public void ParseHttpResponse_ShouldThrowException_WhenETagHeaderIsMissed()
		{
			// Arrange
			var response = new HttpResponseMessage { StatusCode = HttpStatusCode.OK };

			// Act + Assert
			Assert.Throws<CentralConfigFetcher.FailedToFetchConfigException>(
				() => _parser.ParseHttpResponse(response, string.Empty));
		}

		[Fact]
		public void ParseHttpResponse_ShouldReturnEmptyConfigDelta_WhenResponseBodyIsEmpty()
		{
			// Arrange
			var response = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Headers = { ETag = new EntityTagHeaderValue("\"33a64df551425fcc55e4d42a148795d9f25f89d4\"") }
			};

			// Act
			var (configDelta, _) = _parser.ParseHttpResponse(response, "{}");

			// Assert
			configDelta.Should().NotBeNull();
			configDelta.CaptureBody.Should().BeNull();
			configDelta.TransactionMaxSpans.Should().BeNull();
			configDelta.TransactionSampleRate.Should().BeNull();
			configDelta.CaptureBodyContentTypes.Should().BeNull();
		}

		[Fact]
		public void ParseHttpResponse_ShouldLogUnknownKeys()
		{
			// Arrange
			var testLogger = new TestLogger(LogLevel.Information);
			var parser = new CentralConfigResponseParser(testLogger);

			var response = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Headers = { ETag = new EntityTagHeaderValue("\"33a64df551425fcc55e4d42a148795d9f25f89d4\"") }
			};

			// Act
			parser.ParseHttpResponse(response, "{\"unknownKey\": \"value\"}");

			// Assert
			testLogger.Lines.Count.Should().Be(1);
			testLogger.Lines[0]
				.Should()
				.Contain(
					"Central configuration response contains keys that are not in the list of options that can be changed after Agent start: `[unknownKey, value]'");
		}
	}
}
