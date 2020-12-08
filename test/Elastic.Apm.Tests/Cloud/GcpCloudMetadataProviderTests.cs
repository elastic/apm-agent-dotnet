// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Elastic.Apm.Cloud;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using Xunit;
using MockHttpMessageHandler = RichardSzalay.MockHttp.MockHttpMessageHandler;

namespace Elastic.Apm.Tests.Cloud
{
	public class GcpCloudMetadataProviderTests
	{
		[Fact]
		public async Task GetMetadataAsync_Returns_Expected_Cloud_Metadata()
		{
			var stubMetadata = new
			{
				instance = new
				{
					id = 4306570268266786072,
					machineType = "projects/513326162531/machineTypes/n1-standard-1",
					name = "dotnet-agent-test",
					zone = "projects/513326162531/zones/us-west3-a"
				},
				project = new { numericProjectId = 513326162531, projectId = "elastic-apm" }
			};

			var handler = new MockHttpMessageHandler();
			handler.When(GcpCloudMetadataProvider.MetadataUri)
				.Respond("application/json", JsonConvert.SerializeObject(stubMetadata));

			var provider = new GcpCloudMetadataProvider(new NoopLogger(), handler);

			var metadata = await provider.GetMetadataAsync();

			metadata.Should().NotBeNull();
			metadata.Provider.Should().Be(provider.Provider);
			metadata.Instance.Should().NotBeNull();
			metadata.Instance.Id.Should().Be(stubMetadata.instance.id.ToString());
			metadata.Instance.Name.Should().Be(stubMetadata.instance.name);
			metadata.Project.Should().NotBeNull();
			metadata.Project.Id.Should().Be(stubMetadata.project.numericProjectId.ToString());
			metadata.Project.Name.Should().Be(stubMetadata.project.projectId);
			metadata.AvailabilityZone.Should().Be("us-west3-a");
			metadata.Region.Should().Be("us-west3");
			metadata.Machine.Should().NotBeNull();
			metadata.Machine.Type.Should().Be("n1-standard-1");
		}
	}
}
