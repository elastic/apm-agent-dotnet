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
	public class AwsCloudMetadataProviderTests
	{
		[Fact]
		public async Task GetMetadataAsync_Returns_Expected_Cloud_Metadata()
		{
			var stubMetadata = new
			{
				accountId = "946960629917",
				architecture = "x86_64",
				availabilityZone = "us-east-2a",
				billingProducts = (string)null,
				devpayProductCodes = (string)null,
				marketplaceProductCodes = (string)null,
				imageId = "ami-07c1207a9d40bc3bd",
				instanceId = "i-0ae894a7c1c4f2a75",
				instanceType = "t2.medium",
				kernelId = (string)null,
				pendingTime = "2020-06-12T17:46:09Z",
				privateIp = "172.31.0.212",
				ramdiskId = (string)null,
				region = "us-east-2",
				version = "2017-09-30"
			};

			var handler = new MockHttpMessageHandler();
			handler.When(AwsCloudMetadataProvider.TokenUri)
				.Respond("application/json", "aws-token");
			handler.When(AwsCloudMetadataProvider.MetadataUri)
				.Respond("application/json", JsonConvert.SerializeObject(stubMetadata));

			var provider = new AwsCloudMetadataProvider(handler, new NoopLogger());

			var metadata = await provider.GetMetadataAsync();

			metadata.Should().NotBeNull();
			metadata.Account.Should().NotBeNull();
			metadata.Account.Id.Should().Be(stubMetadata.accountId);
			metadata.Provider.Should().Be(provider.Provider);
			metadata.Instance.Should().NotBeNull();
			metadata.Instance.Id.Should().Be(stubMetadata.instanceId);
			metadata.Project.Should().BeNull();
			metadata.AvailabilityZone.Should().Be("us-east-2a");
			metadata.Region.Should().Be(stubMetadata.region);
			metadata.Machine.Should().NotBeNull();
			metadata.Machine.Type.Should().Be(stubMetadata.instanceType);
		}
	}
}
