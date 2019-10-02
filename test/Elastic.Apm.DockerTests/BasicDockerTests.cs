using System.IO;
using Elastic.Apm.Report;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.DockerTests
{
	public class BasicDockerTests
	{
		[Fact]
		public void ContainerIdExistsTest()
		{
			if (!File.Exists("/proc/self/cgroup")) return; //only run in Docker

			using (var agent = new ApmAgent(new AgentComponents()))
			{
				var payloadSender = (agent.PayloadSender as PayloadSenderV2);
				payloadSender.Should().NotBeNull();
				payloadSender?.System.Should().NotBeNull();
				payloadSender?.System.Container.Should().NotBeNull();
				payloadSender?.System.Container.Id.Should().NotBeNullOrWhiteSpace();
			}
		}
	}
}
