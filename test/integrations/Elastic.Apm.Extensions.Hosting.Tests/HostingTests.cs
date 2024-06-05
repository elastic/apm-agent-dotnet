// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Elastic.Apm.Extensions.Tests.Shared;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Extensions.Hosting.Tests
{
	public class HostingTests
	{
		private readonly ExtensionsTestHelper _extensionsTestHelper;

		public HostingTests(ITestOutputHelper output)
		{
			_extensionsTestHelper = new(output);
			_extensionsTestHelper.TestSetup();
		}

		[Fact]
		public async Task AddElasticApm_WhenEnabledIsNotConfigured() =>
			await _extensionsTestHelper.ExecuteTestProcessAsync(null, false, false, false, false);

		[Fact]
		public async Task AddElasticApm_WhenDisabledInConfiguration() =>
			await _extensionsTestHelper.ExecuteTestProcessAsync(false, false, false, false, false);

		[Fact]
		public async Task AddElasticApm_WhenDisabledInEnvironmentVariables() =>
			await _extensionsTestHelper.ExecuteTestProcessAsync(null, false, false, true, false);

		[Fact]
		public async Task AddElasticApm_WhenRegisteredMultipleTimes() =>
			await _extensionsTestHelper.ExecuteTestProcessAsync(null, true, false, true, false);

		[Fact]
		public async Task UseElasticApm_WhenEnabledIsNotConfigured() =>
			await _extensionsTestHelper.ExecuteTestProcessAsync(null, false, true, false, false);

		[Fact]
		public async Task UseElasticApm_WhenDisabledInConfiguration() =>
			await _extensionsTestHelper.ExecuteTestProcessAsync(false, false, true, false, false);

		[Fact]
		public async Task UseElasticApm_WhenDisabledInEnvironmentVariables() =>
			await _extensionsTestHelper.ExecuteTestProcessAsync(null, false, true, true, false);

		[Fact]
		public async Task UseElasticApm_WhenRegisteredMultipleTimes() =>
			await _extensionsTestHelper.ExecuteTestProcessAsync(null, true, true, true, false);

		[Fact]
		public void GetHostingEnvironmentName_WorksViaReflection()
		{
			var environmentName = default(string);
#pragma warning disable CS0618 // Type or member is obsolete
			Host.CreateDefaultBuilder()
				.UseElasticApm()
				.ConfigureServices((ctx, _) =>
				{
					environmentName = HostBuilderExtensions.GetHostingEnvironmentName(ctx, null);
				})
				.Build();
#pragma warning restore CS0618 // Type or member is obsolete

			environmentName.Should().Be("Production");
		}
	}
}
