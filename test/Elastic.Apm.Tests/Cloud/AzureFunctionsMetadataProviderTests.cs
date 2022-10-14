// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Threading.Tasks;
using Elastic.Apm.Cloud;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.Cloud;

public class AzureFunctionsMetadataProviderTests
{
	[Theory]
	[InlineData("~4", "d2cd53b3-acdc-4964-9563-3f5201556a81+wolfgangfaas_group-CentralUSwebspace-Linux", "wolfgangfaas",
		"wolfgangfaas_group", "CentralUS")]
	[InlineData("~4", "d2cd53b3-acdc-4964-9563-3f5201556a81+wolfgangfaas_group-CentralUSwebspace-Linux", "wolfgangfaas",
		"wolfgangfaas_group", null)]
	[InlineData("~4", "d2cd53b3-acdc-4964-9563-3f5201556a81+wolfgangfaas_group-CentralUSwebspace-Linux", "wolfgangfaas",
		null, null)]
	public async Task GetMetadataAsync_Returns_Expected_Cloud_Metadata(string functionsExtensionVersion,
		string websiteOwnerName, string websiteName,
		string resourceGroup, string regionName)
	{
		var environmentVariables = new Hashtable
		{
			{ EnvironmentBasedAzureMetadataProvider.FunctionsExtensionVersion, functionsExtensionVersion },
			{ EnvironmentBasedAzureMetadataProvider.WebsiteOwnerName, websiteOwnerName },
			{ EnvironmentBasedAzureMetadataProvider.WebsiteSiteName, websiteName },
			{ EnvironmentBasedAzureMetadataProvider.WebsiteResourceGroup, resourceGroup },
			{ EnvironmentBasedAzureMetadataProvider.RegionName, regionName }
		};

		var provider = new AzureFunctionsMetadataProvider(new NoopLogger(), environmentVariables);
		var metadata = await provider.GetMetadataAsync();

		metadata.Should().NotBeNull();
		metadata.Account.Should().NotBeNull();
		metadata.Account.Id.Should().Be("d2cd53b3-acdc-4964-9563-3f5201556a81");
		metadata.Provider.Should().Be("azure");
		metadata.Instance.Name.Should().Be("wolfgangfaas");
		metadata.Project.Should().NotBeNull();
		metadata.Project.Name.Should().Be("wolfgangfaas_group");
		metadata.Region.Should().Be("CentralUS");
	}


	[Theory]
	[InlineData(null, "d2cd53b3-acdc-4964-9563-3f5201556a81+wolfgangfaas_group-CentralUSwebspace-Linux", "wolfgangfaas",
		null, null)]
	[InlineData("~4", "d2cd53b3-acdc-4964-9563-3f5201556a81+wolfgangfaas_group-CentralUSwebspace-Linux", null, null,
		null)]
	[InlineData("~4", null, "wolfgangfaas", null, null)]
	[InlineData("~4", "foo", "wolfgangfaas", null,
		null)]
	[InlineData("~4", "d2cd53b3-acdc-4964-9563-3f5201556a81*wolfgangfaas_group-CentralUSwebspace-Linux", "wolfgangfaas",
		null,
		null)]
	public async Task GetMetadataAsync_Returns_Null_When_Expected_EnvironmentVariable_Is_Missing_Or_Corrupt(
		string functionsExtensionVersion,
		string websiteOwnerName, string websiteName,
		string resourceGroup, string regionName)
	{
		var environmentVariables = new Hashtable
		{
			{ EnvironmentBasedAzureMetadataProvider.FunctionsExtensionVersion, functionsExtensionVersion },
			{ EnvironmentBasedAzureMetadataProvider.WebsiteOwnerName, websiteOwnerName },
			{ EnvironmentBasedAzureMetadataProvider.WebsiteSiteName, websiteName },
			{ EnvironmentBasedAzureMetadataProvider.WebsiteResourceGroup, resourceGroup },
			{ EnvironmentBasedAzureMetadataProvider.RegionName, regionName }
		};

		var provider = new AzureFunctionsMetadataProvider(new NoopLogger(), environmentVariables);
		var metadata = await provider.GetMetadataAsync();

		metadata.Should().BeNull();
	}

	[Fact]
	public async Task GetMetadataAsync_Returns_Null_When_EnvironmentVariables_Is_Null()
	{
		var provider = new AzureFunctionsMetadataProvider(new NoopLogger(), null);
		var metadata = await provider.GetMetadataAsync();

		metadata.Should().BeNull();
	}
}
