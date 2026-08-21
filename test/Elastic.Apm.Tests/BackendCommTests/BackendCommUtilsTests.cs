// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
#if NET462
using System.Net;
#else
using System.Net.Http;
using System.Security.Authentication;
#endif
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static Elastic.Apm.BackendComm.BackendCommUtils.ApmServerEndpoints;
using static Elastic.Apm.Tests.Utilities.FluentAssertionsUtils;

// ReSharper disable ImplicitlyCapturedClosure

namespace Elastic.Apm.Tests.BackendCommTests
{
	public class BackendCommUtilsTests : LoggingTestBase
	{
		public BackendCommUtilsTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

#if NET462
		[Theory]
		[InlineData(0, 0)]
		[InlineData((int)SecurityProtocolType.Tls, (int)(SecurityProtocolType.Tls | SecurityProtocolType.Tls12))]
		[InlineData((int)SecurityProtocolType.Tls12, (int)SecurityProtocolType.Tls12)]
		public void EnsureTls12ForExplicitSecurityProtocols_preserves_system_default_and_adds_tls12(int input, int expected) =>
			BackendCommUtils.EnsureTls12ForExplicitSecurityProtocols((SecurityProtocolType)input)
				.Should()
				.Be((SecurityProtocolType)expected);
#else
		[Fact]
		public void CreateHttpClientHandler_uses_os_default_tls_protocols()
		{
			using var handler = BackendCommUtils.CreateHttpClientHandler(new MockConfiguration(), new NoopLogger());

			handler.SslProtocols.Should().Be(SslProtocols.None);
		}
#endif

		[Theory]
		[InlineData("http://1.2.3.4", "My svc", "My env", "http://1.2.3.4/config/v1/agents?service.name=My+svc&service.environment=My+env")]
		[InlineData("http://1.2.3.4/", "My svc", "My env", "http://1.2.3.4/config/v1/agents?service.name=My+svc&service.environment=My+env")]
		[InlineData("http://1.2.3.4:8200", "My svc", "My env", "http://1.2.3.4:8200/config/v1/agents?service.name=My+svc&service.environment=My+env")]
		[InlineData("http://1.2.3.4:8200/", "My svc", "My env",
			"http://1.2.3.4:8200/config/v1/agents?service.name=My+svc&service.environment=My+env")]
		[InlineData("http://1.2.3.4/base_relative_path", "My svc", "My env",
			"http://1.2.3.4/base_relative_path/config/v1/agents?service.name=My+svc&service.environment=My+env")]
		[InlineData("http://1.2.3.4/base_relative_path/", "My svc", "My env",
			"http://1.2.3.4/base_relative_path/config/v1/agents?service.name=My+svc&service.environment=My+env")]
		[InlineData("http://1.2.3.4/base/relative/path", "My svc", "My env",
			"http://1.2.3.4/base/relative/path/config/v1/agents?service.name=My+svc&service.environment=My+env")]
		[InlineData("http://1.2.3.4/base/relative/path/", "My svc", "My env",
			"http://1.2.3.4/base/relative/path/config/v1/agents?service.name=My+svc&service.environment=My+env")]
		[InlineData("http://1.2.3.4", null, null, "http://1.2.3.4/config/v1/agents")]
		[InlineData("http://1.2.3.4/", null, null, "http://1.2.3.4/config/v1/agents")]
		[InlineData("http://1.2.3.4/base_relative_path", null, null, "http://1.2.3.4/base_relative_path/config/v1/agents")]
		[InlineData("http://1.2.3.4/base_relative_path/", null, null, "http://1.2.3.4/base_relative_path/config/v1/agents")]
		[InlineData("http://1.2.3.4:8200", "My svc", "My env amp:(&) plus:(+) ang:(<>) eq:(=) qm:(?)"
			, "http://1.2.3.4:8200/config/v1/agents?service.name=My+svc&service.environment=My+env+amp%3A%28%26%29+plus%3A%28%2B%29+ang%3A%28%3C%3E%29+eq%3A%28%3D%29+qm%3A%28%3F%29")]
		[InlineData("https://5.6.7.8:9", "My svc", null, "https://5.6.7.8:9/config/v1/agents?service.name=My+svc")]
		[InlineData("https://1.2.3.4/", null, "My env", "https://1.2.3.4/config/v1/agents?service.environment=My+env")]
		public void BuildGetConfigAbsoluteUrl_tests(string serverBaseUrl, string serviceName, string envName, string expectedGetConfigApiAbsoluteUrl)
		{
			var actualGetConfigApiAbsoluteUrl =
				BuildGetConfigAbsoluteUrl(new Uri(serverBaseUrl, UriKind.Absolute), BuildService(serviceName, envName));
			actualGetConfigApiAbsoluteUrl.IsAbsoluteUri.Should().BeTrue($"{nameof(actualGetConfigApiAbsoluteUrl)}: {actualGetConfigApiAbsoluteUrl}");
			actualGetConfigApiAbsoluteUrl.Should().Be(expectedGetConfigApiAbsoluteUrl);
		}

		private static Service BuildService(string serviceName, string envName)
		{
			var service = Service.GetDefaultService(new MockConfiguration(), new NoopLogger());
			service.Environment = envName;
			service.Name = serviceName;
			return service;
		}

		[Fact]
		public void BuildGetConfigAbsoluteUrl_throws_on_not_absolute_base() =>
			AsAction(() => BuildGetConfigAbsoluteUrl(new Uri("relative_URL", UriKind.Relative), BuildService("My svc", "My env")))
				.Should()
				.ThrowExactly<ArgumentException>()
				.WithMessage("*should*be*absolute*");

		[Theory]
		[InlineData("http://1.2.3.4", "http://1.2.3.4/intake/v2/events")]
		[InlineData("http://1.2.3.4/", "http://1.2.3.4/intake/v2/events")]
		[InlineData("http://1.2.3.4/base_relative_path", "http://1.2.3.4/base_relative_path/intake/v2/events")]
		[InlineData("http://1.2.3.4/base_relative_path/", "http://1.2.3.4/base_relative_path/intake/v2/events")]
		[InlineData("http://1.2.3.4/base/relative/path", "http://1.2.3.4/base/relative/path/intake/v2/events")]
		[InlineData("http://1.2.3.4/base/relative/path/", "http://1.2.3.4/base/relative/path/intake/v2/events")]
		public void BuildIntakeV2EventsAbsoluteUrl_normal_cases(string serverBaseUrl, string expectedIntakeApiAbsoluteUrl)
		{
			var actualGetConfigApiAbsoluteUrl = BuildIntakeV2EventsAbsoluteUrl(new Uri(serverBaseUrl, UriKind.Absolute));
			actualGetConfigApiAbsoluteUrl.IsAbsoluteUri.Should().BeTrue($"{nameof(actualGetConfigApiAbsoluteUrl)}: {actualGetConfigApiAbsoluteUrl}");
			actualGetConfigApiAbsoluteUrl.Should().Be(expectedIntakeApiAbsoluteUrl);
		}

		[Fact]
		public void BuildIntakeV2EventsAbsoluteUrl_throws_on_not_absolute_base() =>
			AsAction(() => BuildIntakeV2EventsAbsoluteUrl(new Uri("relative_URL", UriKind.Relative)))
				.Should()
				.ThrowExactly<ArgumentException>()
				.WithMessage("*should*be*absolute*");
	}
}
