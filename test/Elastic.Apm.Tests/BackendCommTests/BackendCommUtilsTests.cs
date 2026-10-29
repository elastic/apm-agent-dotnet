// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Net;
#if NET
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
#endif
using System.Threading;
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
#endif

		[Theory]
		[InlineData(0)]
		[InlineData(-1)]
		public void ToPooledConnectionLifetime_zero_disables_recycling(int milliseconds) =>
			BackendCommUtils.ToPooledConnectionLifetime(TimeSpan.FromMilliseconds(milliseconds)).Should().Be(Timeout.InfiniteTimeSpan);

		[Theory]
		[InlineData(1)]
		[InlineData(60_000)]
		public void ToPooledConnectionLifetime_passes_through_positive_values(int milliseconds) =>
			BackendCommUtils.ToPooledConnectionLifetime(TimeSpan.FromMilliseconds(milliseconds)).Should().Be(TimeSpan.FromMilliseconds(milliseconds));

		// ServicePoint.ConnectionLeaseTimeout: -1 is infinite, whereas 0 closes the connection after every request.
		[Fact]
		public void ToConnectionLeaseTimeout_zero_maps_to_infinite_and_not_zero() =>
			BackendCommUtils.ToConnectionLeaseTimeout(TimeSpan.Zero).Should().Be(Timeout.Infinite);

		[Theory]
		[InlineData(1, 1)]
		[InlineData(60_000, 60_000)]
		[InlineData(int.MaxValue, int.MaxValue)]
		public void ToConnectionLeaseTimeout_passes_through_positive_values(int milliseconds, int expected) =>
			BackendCommUtils.ToConnectionLeaseTimeout(TimeSpan.FromMilliseconds(milliseconds)).Should().Be(expected);

		[Fact]
		public void ToConnectionLeaseTimeout_clamps_values_that_do_not_fit_in_an_int() =>
			BackendCommUtils.ToConnectionLeaseTimeout(TimeSpan.FromDays(30)).Should().Be(int.MaxValue);

#if NET
		private static SocketsHttpHandler CreateHandler(MockConfiguration configuration) =>
			BackendCommUtils.CreateHttpMessageHandler(configuration, new NoopLogger()).Should().BeOfType<SocketsHttpHandler>().Subject;

		[Fact]
		public void CreateHttpMessageHandler_recycles_connections_every_minute_by_default()
		{
			using var handler = CreateHandler(new MockConfiguration());

			handler.PooledConnectionLifetime.Should().Be(TimeSpan.FromMinutes(1));
		}

		[Theory]
		[InlineData("5m", 5 * 60 * 1000)]
		[InlineData("30s", 30 * 1000)]
		[InlineData("0", -1)]
		[InlineData("-1", 60 * 1000)] // invalid, falls back to the default
		public void CreateHttpMessageHandler_applies_server_connection_lifetime(string configValue, int expectedMilliseconds)
		{
			using var handler = CreateHandler(new MockConfiguration(serverConnectionLifetime: configValue));

			handler.PooledConnectionLifetime.Should().Be(TimeSpan.FromMilliseconds(expectedMilliseconds));
		}

		[Fact]
		public void CreateHttpMessageHandler_uses_os_default_tls_protocols()
		{
			using var handler = CreateHandler(new MockConfiguration());

			handler.SslOptions.EnabledSslProtocols.Should().Be(SslProtocols.None);
		}

		[Theory]
		[InlineData("false", false)]
		[InlineData("true", true)]
		public void CreateHttpMessageHandler_uses_default_credentials_only_when_windows_credentials_are_enabled(string configValue, bool expected)
		{
			using var handler = CreateHandler(new MockConfiguration(useWindowsCredentials: configValue));

			if (expected)
				handler.Credentials.Should().BeSameAs(CredentialCache.DefaultCredentials);
			else
				handler.Credentials.Should().BeNull();
		}

		[Fact]
		public void CreateHttpMessageHandler_does_not_override_the_default_proxy_when_none_is_configured()
		{
			using var handler = CreateHandler(new MockConfiguration());

			handler.UseProxy.Should().BeTrue();
			handler.Proxy.Should().BeNull();
		}

		[Fact]
		public void CreateHttpMessageHandler_configures_proxy_with_credentials()
		{
			using var handler = CreateHandler(new MockConfiguration(proxyUrl: "http://proxy.local:8080", proxyUserName: "user", proxyPassword: "pass"));

			handler.UseProxy.Should().BeTrue();
			var proxy = handler.Proxy.Should().BeOfType<WebProxy>().Subject;
			proxy.Address.Should().Be(new Uri("http://proxy.local:8080"));
			var credential = proxy.Credentials.GetCredential(proxy.Address, "Basic");
			credential.UserName.Should().Be("user");
			credential.Password.Should().Be("pass");
		}

		[Fact]
		public void CreateHttpMessageHandler_configures_proxy_without_credentials()
		{
			using var handler = CreateHandler(new MockConfiguration(proxyUrl: "http://proxy.local:8080"));

			var proxy = handler.Proxy.Should().BeOfType<WebProxy>().Subject;
			proxy.Address.Should().Be(new Uri("http://proxy.local:8080"));
			proxy.Credentials.Should().BeNull();
		}

		[Theory]
		[InlineData(null, SslPolicyErrors.None, true)]
		[InlineData(null, SslPolicyErrors.RemoteCertificateNameMismatch, false)]
		[InlineData("true", SslPolicyErrors.None, true)]
		[InlineData("true", SslPolicyErrors.RemoteCertificateNameMismatch, false)]
		[InlineData("false", SslPolicyErrors.RemoteCertificateNameMismatch, true)]
		public void CreateHttpMessageHandler_certificate_validation_honours_verify_server_cert(string verifyServerCert, SslPolicyErrors errors, bool expected)
		{
			using var handler = CreateHandler(new MockConfiguration(verifyServerCert: verifyServerCert));

			var callback = handler.SslOptions.RemoteCertificateValidationCallback;
			callback.Should().NotBeNull();
			callback(this, null, null, errors).Should().Be(expected);
		}

		[Fact]
		public void CreateHttpMessageHandler_falls_back_to_default_validation_when_server_cert_cannot_be_loaded()
		{
			using var handler = CreateHandler(new MockConfiguration(serverCert: "does-not-exist.pem"));

			handler.SslOptions.RemoteCertificateValidationCallback.Should().BeNull();
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
