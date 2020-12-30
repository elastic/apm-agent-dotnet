// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.TestHelpers;
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
			var service = Service.GetDefaultService(new MockConfigSnapshot(), new NoopLogger());
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
