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
			var kubernetesInfo = _systemInfoHelper.ParseKubernetesInfo(null, null);

			// Assert
			kubernetesInfo.Should().BeNull();
		}

		[Fact]
		public void ParseKubernetesInfo_ShouldUseContainerInfoAndHostName_WhenNoEnvironmentVariablesAreSet()
		{
			// Arrange
			var containerInfo = new Container { Id = "containerId" };
			const string hostName = "hostName";

			// Act
			var kubernetesInfo = _systemInfoHelper.ParseKubernetesInfo(containerInfo, hostName);

			// Assert
			kubernetesInfo.Should().NotBeNull();
			kubernetesInfo.Node.Should().BeNull();
			kubernetesInfo.Namespace.Should().BeNull();
			kubernetesInfo.Pod.Should().NotBeNull();
			kubernetesInfo.Pod.Uid.Should().Be(containerInfo.Id);
			kubernetesInfo.Pod.Name.Should().Be(hostName);
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
			var kubernetesInfo = _systemInfoHelper.ParseKubernetesInfo(null, null);

			// Assert
			kubernetesInfo.Should().NotBeNull();
			validateAction.Invoke(kubernetesInfo);
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
