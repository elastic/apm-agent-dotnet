using System;
using System.Linq;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class UrlUtilsTests
	{
		public const int DefaultHttpPort = 80;
		public const int DefaultHttpsPort = 443;

		[Theory]
		[InlineData("https://elastic.co/foo/bar", "elastic.co", DefaultHttpsPort)]
		[InlineData("http://elastic.co/foo/bar", "elastic.co", DefaultHttpPort)]
		// unknown scheme and thus no default port
		[InlineData("dummyscheme://elastic.co/foo/bar", "elastic.co", null)]
		[InlineData("dummyscheme://elastic.co:1234/foo/bar", "elastic.co", 1234)]
		[InlineData("dummyscheme://elastic.co", "elastic.co", null)]
		[InlineData("dummyscheme://elastic.co:1234", "elastic.co", 1234)]
		// IPv4
		[InlineData("http://1.2.3.4:56789", "1.2.3.4", 56789)]
		// IPv6
		[InlineData("http://[::1]:8080/", "::1", 8080)]
		[InlineData("http://[2012:b86a:f950::b86a:f950]:8080/", "2012:b86a:f950::b86a:f950", 8080)]
		[InlineData("http://[2012:b86a:f950::b86a:f950]", "2012:b86a:f950::b86a:f950", DefaultHttpPort)]
		// IPv6 with zone indices
		// https://en.wikipedia.org/wiki/IPv6_address#Use_of_zone_indices_in_URIs
		[InlineData("https://[fe80::200:39ff:fe36:1a2d%254]:54321/temp/example.htm", "fe80::200:39ff:fe36:1a2d", 54321)]
		[InlineData("https://[fe80::200:39ff:fe36:1a2d%254]", "fe80::200:39ff:fe36:1a2d", DefaultHttpsPort)]
		// Non-ASCII characters in host name
		[InlineData("http://München", "münchen", DefaultHttpPort)]
		[InlineData("http://Хост", "Хост", DefaultHttpPort)] // Host in Russian
		public void TryExtractDestinationInfo_valid_input(string inputUrl, string expectedHost, int? expectedPort)
		{
			UrlUtils.TryExtractDestinationInfo(new Uri(inputUrl), out var actualHost, out var actualPort, new NoopLogger()).Should().BeTrue();
			actualHost.Should().Be(expectedHost);
			actualPort.Should().Be(expectedPort);
		}

		[Theory]
		[InlineData("C:\\")] // no host (Basic host name type)
		public void TryExtractDestinationInfo_invalid_input(string inputUrl)
		{
			var mockLogger = new TestLogger(LogLevel.Trace);
			UrlUtils.TryExtractDestinationInfo(new Uri(inputUrl), out var actualHost, out var actualPort, mockLogger).Should().BeFalse();
			mockLogger.Lines.Should().Contain(line => line.Contains(nameof(UrlUtils)) && line.Contains(nameof(UrlUtils.TryExtractDestinationInfo)));
		}
	}
}
