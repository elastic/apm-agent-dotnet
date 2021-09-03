// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Api.Kubernetes;
using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class SystemInfoHelperTests : IDisposable
	{
		private readonly SystemInfoHelper _systemInfoHelper;

		public SystemInfoHelperTests() => _systemInfoHelper = new SystemInfoHelper(new NoopLogger());

		[Fact]
		public void ParseSystemInfo_Should_Use_HostName_For_ConfiguredHostName()
		{
			var hostName = "This_is_my_host";
			var system = _systemInfoHelper.ParseSystemInfo(hostName);

#pragma warning disable 618
			system.HostName.Should().Be(hostName);
#pragma warning restore 618

			system.ConfiguredHostName.Should().Be(hostName);
			system.DetectedHostName.Should().NotBe(hostName);
		}

		[Fact]
		public void ParseKubernetesInfo_ShouldReturnNull_WhenNoEnvironmentVariablesAreSetAndContainerInfoIsNull()
		{
			// Arrange + Act
			var system = new Api.System();
			_systemInfoHelper.ParseKubernetesInfo(system);

			// Assert
			system.Kubernetes.Should().BeNull();
		}

		[Theory]
		// underscores
		[InlineData("1:name=systemd:/kubepods.slice/kubepods-burstable.slice/" +
			"kubepods-burstable-pod90d81341_92de_11e7_8cf2_507b9d4141fa.slice/" +
			"crio-2227daf62df6694645fee5df53c1f91271546a9560e8600a525690ae252b7f63.scope",
			"2227daf62df6694645fee5df53c1f91271546a9560e8600a525690ae252b7f63", "90d81341-92de-11e7-8cf2-507b9d4141fa")]
		// openshift form
		[InlineData("9:freezer:/kubepods.slice/kubepods-pod22949dce_fd8b_11ea_8ede_98f2b32c645c.slice" +
			"/docker-b15a5bdedd2e7645c3be271364324321b908314e4c77857bbfd32a041148c07f.scope",
			"b15a5bdedd2e7645c3be271364324321b908314e4c77857bbfd32a041148c07f",
			"22949dce-fd8b-11ea-8ede-98f2b32c645c")]
		// ubuntu cgroup
		[InlineData("1:name=systemd:/user.slice/user-1000.slice/user@1000.service/apps.slice/apps-org.gnome.Terminal" +
			".slice/vte-spawn-75bc72bd-6642-4cf5-b62c-0674e11bfc84.scope", null, null)]
		public void ParseKubernetesInfo_FromCGroupLine(string line, string containerId, string podId)
		{
			var system = new Api.System();
			_systemInfoHelper.ParseContainerId(system, "hostname", line);

			if (containerId is null)
				system.Container.Should().BeNull();
			else
				system.Container.Id.Should().Be(containerId);

			if (podId is null)
				system.Kubernetes.Should().BeNull();
			else
				system.Kubernetes.Pod.Uid.Should().Be(podId);
		}

		[Fact]
		public void ParseKubernetesInfo_ShouldUseContainerInfoAndHostName_WhenNoEnvironmentVariablesAreSet()
		{
			// Arrange
			var containerId = "e9b90526-f47d-11e8-b2a5-080027b9f4fb";
			var hostName = "hostName";
			var line = $"1:name=systemd:/kubepods/besteffort/pod{containerId}/15aa6e53-b09a-40c7-8558-c6c31e36c88a";

			// Act
			_systemInfoHelper.ParseSystemInfo(hostName);

			var system = new Api.System();
			_systemInfoHelper.ParseContainerId(system, hostName, line);
			_systemInfoHelper.ParseKubernetesInfo(system);

			// Assert
			system.Kubernetes.Should().NotBeNull();
			system.Kubernetes.Node.Should().BeNull();
			system.Kubernetes.Namespace.Should().BeNull();
			system.Kubernetes.Pod.Should().NotBeNull();
			system.Kubernetes.Pod.Uid.Should().Be(containerId);
			system.Kubernetes.Pod.Name.Should().Be(hostName);
		}

		public static IEnumerable<object[]> EnvironmentVariablesData
		{
			get
			{
				const string podUid = "podUid";
				const string podName = "podName";
				const string nodeName = "nodeName";
				const string @namespace = "namespace";

				yield return new object[]
				{
					SystemInfoHelper.PodUid, podUid, new Action<KubernetesMetadata>(kubernetesInfo =>
					{
						kubernetesInfo.Node.Should().BeNull();
						kubernetesInfo.Namespace.Should().BeNull();

						kubernetesInfo.Pod.Should().NotBeNull();
						kubernetesInfo.Pod.Name.Should().BeNull();
						kubernetesInfo.Pod.Uid.Should().Be(podUid);
					})
				};
				yield return new object[]
				{
					SystemInfoHelper.PodName, podName, new Action<KubernetesMetadata>(kubernetesInfo =>
					{
						kubernetesInfo.Node.Should().BeNull();
						kubernetesInfo.Namespace.Should().BeNull();

						kubernetesInfo.Pod.Should().NotBeNull();
						kubernetesInfo.Pod.Uid.Should().BeNull();
						kubernetesInfo.Pod.Name.Should().Be(podName);
					})
				};
				yield return new object[]
				{
					SystemInfoHelper.NodeName, nodeName, new Action<KubernetesMetadata>(kubernetesInfo =>
					{
						kubernetesInfo.Pod.Should().BeNull();
						kubernetesInfo.Namespace.Should().BeNull();

						kubernetesInfo.Node.Should().NotBeNull();
						kubernetesInfo.Node.Name.Should().Be(nodeName);
					})
				};
				yield return new object[]
				{
					SystemInfoHelper.Namespace, @namespace, new Action<KubernetesMetadata>(kubernetesInfo =>
					{
						kubernetesInfo.Pod.Should().BeNull();
						kubernetesInfo.Node.Should().BeNull();

						kubernetesInfo.Namespace.Should().Be(@namespace);
					})
				};
			}
		}

		[Theory]
		[MemberData(nameof(EnvironmentVariablesData))]
		public void ParseKubernetesInfo_ShouldUseInformationFromEnvironmentVariables_WhenAtLeastOneVariableIsSet(string name, string value,
			Action<KubernetesMetadata> validateAction
		)
		{
			// Arrange
			Environment.SetEnvironmentVariable(name, value);

			// Act
			var system = new Api.System();
			_systemInfoHelper.ParseKubernetesInfo(system);

			// Assert
			system.Kubernetes.Should().NotBeNull();
			validateAction.Invoke(system.Kubernetes);
		}

		public void Dispose()
		{
			Environment.SetEnvironmentVariable(SystemInfoHelper.PodUid, null);
			Environment.SetEnvironmentVariable(SystemInfoHelper.PodName, null);
			Environment.SetEnvironmentVariable(SystemInfoHelper.NodeName, null);
			Environment.SetEnvironmentVariable(SystemInfoHelper.Namespace, null);
		}
	}
}
