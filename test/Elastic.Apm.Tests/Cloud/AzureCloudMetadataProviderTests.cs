// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Elastic.Apm.Cloud;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using Xunit;
using MockHttpMessageHandler = RichardSzalay.MockHttp.MockHttpMessageHandler;

namespace Elastic.Apm.Tests.Cloud
{
	public class AzureCloudMetadataProviderTests
	{
		[Fact]
		public async Task GetMetadataAsync_Returns_Expected_Cloud_Metadata()
		{
			var stubMetadata = new
			{
				location = "westus2",
				name = "dotnet-agent-test",
				resourceGroupName = "dotnet-agent-testing",
				subscriptionId = "7657426d-c4c3-44ac-88a2-3b2cd59e6dba",
				vmId = "e11ebedc-019d-427f-84dd-56cd4388d3a8",
				vmSize = "Standard_D2s_v3",
				zone = "zone-1"
			};

			var handler = new MockHttpMessageHandler();
			handler.When(AzureCloudMetadataProvider.MetadataUri)
				.Respond("application/json", JsonConvert.SerializeObject(stubMetadata));

			var provider = new AzureCloudMetadataProvider(new NoopLogger(), handler);

			var metadata = await provider.GetMetadataAsync();

			metadata.Should().NotBeNull();
			metadata.Account.Should().NotBeNull();
			metadata.Account.Id.Should().Be(stubMetadata.subscriptionId);
			metadata.Provider.Should().Be(provider.Provider);
			metadata.Instance.Should().NotBeNull();
			metadata.Instance.Id.Should().Be(stubMetadata.vmId);
			metadata.Instance.Name.Should().Be(stubMetadata.name);
			metadata.Project.Should().NotBeNull();
			metadata.Project.Name.Should().Be(stubMetadata.resourceGroupName);
			metadata.Region.Should().Be(stubMetadata.location);
			metadata.Machine.Should().NotBeNull();
			metadata.Machine.Type.Should().Be(stubMetadata.vmSize);
			metadata.AvailabilityZone.Should().Be(stubMetadata.zone);
		}

		[Fact]
		public async Task GetMetadataAsync_Returns_Expected_Cloud_Metadata_When_No_Zone()
		{
			var stubMetadata = new
			{
				location = "westus2",
				name = "dotnet-agent-test",
				resourceGroupName = "dotnet-agent-testing",
				subscriptionId = "7657426d-c4c3-44ac-88a2-3b2cd59e6dba",
				vmId = "e11ebedc-019d-427f-84dd-56cd4388d3a8",
				vmSize = "Standard_D2s_v3"
			};

			var handler = new MockHttpMessageHandler();
			handler.When(AzureCloudMetadataProvider.MetadataUri)
				.Respond("application/json", JsonConvert.SerializeObject(stubMetadata));

			var provider = new AzureCloudMetadataProvider(new NoopLogger(), handler);

			var metadata = await provider.GetMetadataAsync();

			metadata.Should().NotBeNull();
			metadata.Account.Should().NotBeNull();
			metadata.Account.Id.Should().Be(stubMetadata.subscriptionId);
			metadata.Provider.Should().Be(provider.Provider);
			metadata.Instance.Should().NotBeNull();
			metadata.Instance.Id.Should().Be(stubMetadata.vmId);
			metadata.Instance.Name.Should().Be(stubMetadata.name);
			metadata.Project.Should().NotBeNull();
			metadata.Project.Name.Should().Be(stubMetadata.resourceGroupName);
			metadata.Region.Should().Be(stubMetadata.location);
			metadata.Machine.Should().NotBeNull();
			metadata.Machine.Type.Should().Be(stubMetadata.vmSize);
			metadata.AvailabilityZone.Should().BeNull();
		}
	}
}
