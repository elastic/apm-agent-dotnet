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

public class AzureAppServiceMetadataProviderTests
{
	[Fact]
	public async Task GetMetadataAsync_Returns_Expected_Cloud_Metadata()
	{
		var environmentVariables = new Hashtable
		{
			{ AzureEnvironmentVariables.WebsiteInstanceId, "instance_id" },
			{
				AzureEnvironmentVariables.WebsiteOwnerName,
				"f5940f10-2e30-3e4d-a259-63451ba6dae4+elastic-apm-AustraliaEastwebspace"
			},
			{ AzureEnvironmentVariables.WebsiteSiteName, "site_name" },
			{ AzureEnvironmentVariables.WebsiteResourceGroup, "resource_group" }
		};

		var provider = new AzureAppServiceMetadataProvider(new NoopLogger(), environmentVariables);
		var metadata = await provider.GetMetadataAsync();

		metadata.Should().NotBeNull();
		metadata.Account.Should().NotBeNull();
		metadata.Account.Id.Should().Be("f5940f10-2e30-3e4d-a259-63451ba6dae4");
		metadata.Provider.Should().Be("azure");
		metadata.Instance.Should().NotBeNull();
		metadata.Instance.Id.Should().Be("instance_id");
		metadata.Instance.Name.Should().Be("site_name");
		metadata.Project.Should().NotBeNull();
		metadata.Project.Name.Should().Be("resource_group");
		metadata.Region.Should().Be("AustraliaEast");
	}

	[Theory]
	[InlineData(null, "f5940f10-2e30-3e4d-a259-63451ba6dae4+elastic-apm-AustraliaEastwebspace", "site_name",
		"resource_group")]
	[InlineData("instance_id", null, "site_name", "resource_group")]
	[InlineData("instance_id", "f5940f10-2e30-3e4d-a259-63451ba6dae4+elastic-apm-AustraliaEastwebspace", null,
		"resource_group")]
	[InlineData("instance_id", "f5940f10-2e30-3e4d-a259-63451ba6dae4+elastic-apm-AustraliaEastwebspace", "site_name",
		null)]
	public async Task GetMetadataAsync_Returns_Null_When_Expected_EnvironmentVariable_Is_Missing(
		string instanceId, string ownerName, string siteName, string resourceGroup)
	{
		var environmentVariables = new Hashtable
		{
			{ AzureEnvironmentVariables.WebsiteInstanceId, instanceId },
			{ AzureEnvironmentVariables.WebsiteOwnerName, ownerName },
			{ AzureEnvironmentVariables.WebsiteSiteName, siteName },
			{ AzureEnvironmentVariables.WebsiteResourceGroup, resourceGroup }
		};

		var provider = new AzureAppServiceMetadataProvider(new NoopLogger(), environmentVariables);
		var metadata = await provider.GetMetadataAsync();

		metadata.Should().BeNull();
	}

	[Fact]
	public async Task GetMetadataAsync_Returns_Null_When_EnvironmentVariables_Is_Null()
	{
		var provider = new AzureAppServiceMetadataProvider(new NoopLogger(), null);
		var metadata = await provider.GetMetadataAsync();

		metadata.Should().BeNull();
	}
}
