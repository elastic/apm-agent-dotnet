// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using Elastic.Apm.Cloud;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Config.ConfigConsts;

namespace Elastic.Apm.Tests.Cloud
{
	public class CloudMetadataProviderCollectionTests
	{
		[Fact]
		public void DefaultCloudProvider_Registers_Aws_Gcp_Azure_Providers()
		{
			var providers = new CloudMetadataProviderCollection(DefaultValues.CloudProvider, new NoopLogger());

			providers.Count.Should().Be(3);
			providers.TryGetValue(AwsCloudMetadataProvider.Name, out _).Should().BeTrue();
			providers.TryGetValue(GcpCloudMetadataProvider.Name, out _).Should().BeTrue();
			providers.TryGetValue(AzureCloudMetadataProvider.Name, out _).Should().BeTrue();
			providers.Select(p => p.Provider).Should().Equal("aws", "gcp", "azure");
		}

		[Fact]
		public void CloudProvider_None_Should_Not_Register_Any_Providers()
		{
			var providers = new CloudMetadataProviderCollection(SupportedValues.CloudProviderNone, new NoopLogger());
			providers.Count.Should().Be(0);
		}

		[Fact]
		public void CloudProvider_Aws_Should_Register_Aws_Provider()
		{
			var providers = new CloudMetadataProviderCollection(SupportedValues.CloudProviderAws, new NoopLogger());
			providers.Count.Should().Be(1);
			providers.TryGetValue(SupportedValues.CloudProviderAws, out var provider).Should().BeTrue();
			provider.Should().BeOfType<AwsCloudMetadataProvider>();
		}

		[Fact]
		public void CloudProvider_Gcp_Should_Register_Gcp_Provider()
		{
			var providers = new CloudMetadataProviderCollection(SupportedValues.CloudProviderGcp, new NoopLogger());
			providers.Count.Should().Be(1);
			providers.TryGetValue(SupportedValues.CloudProviderGcp, out var provider).Should().BeTrue();
			provider.Should().BeOfType<GcpCloudMetadataProvider>();
		}

		[Fact]
		public void CloudProvider_Azure_Should_Register_Azure_Provider()
		{
			var providers = new CloudMetadataProviderCollection(SupportedValues.CloudProviderAzure, new NoopLogger());
			providers.Count.Should().Be(1);
			providers.TryGetValue(SupportedValues.CloudProviderAzure, out var provider).Should().BeTrue();
			provider.Should().BeOfType<AzureCloudMetadataProvider>();
		}
	}
}
