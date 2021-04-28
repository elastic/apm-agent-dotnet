using System.Collections.Generic;
using Docker.DotNet;
using Microsoft.Extensions.Logging;

namespace TestEnvironment.Docker.Containers.Mail
{
    public class MailContainer : Container
    {
        public MailContainer(
            DockerClient dockerClient,
            string name,
            string imageName = "mailhog/mailhog",
            string tag = "latest",
            ushort smptPort = 1025,
            ushort apiPort = 8025,
            string deleteEndpoint = "api/v1/messages",
            IDictionary<string, string> environmentVariables = null,
            IDictionary<ushort, ushort> ports = null,
            bool isDockerInDocker = false,
            bool reuseContainer = false,
            ILogger logger = null)
            : base(
                  dockerClient,
                  name,
                  imageName,
                  tag,
                  environmentVariables,
                  ports,
                  isDockerInDocker,
                  reuseContainer,
                  new MailContainerWaiter(smptPort, logger),
                  new MailContainerCleaner(apiPort, deleteEndpoint, logger: logger),
                  logger)
        {
        }
    }
}
