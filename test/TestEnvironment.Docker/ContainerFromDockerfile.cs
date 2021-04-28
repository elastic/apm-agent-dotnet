using System.Collections.Generic;
using Docker.DotNet;
using Microsoft.Extensions.Logging;

namespace TestEnvironment.Docker
{
    public class ContainerFromDockerfile : Container
    {
        public ContainerFromDockerfile(
            DockerClient dockerClient,
            string name,
            string dockerfile,
            IDictionary<string, string> buildArgs = null,
            string context = ".",
            IDictionary<string, string> environmentVariables = null,
            IDictionary<ushort, ushort> ports = null,
            bool isDockerInDocker = false,
            bool reuseContainer = false,
            IContainerWaiter containerWaiter = null,
            IContainerCleaner containerCleaner = null,
            ILogger logger = null)
            : base(dockerClient, name, name, "dev", environmentVariables, ports, isDockerInDocker, reuseContainer, containerWaiter, containerCleaner, logger)
        {
            Dockerfile = dockerfile;
            BuildArgs = buildArgs;
            Context = context;
        }

        public string Dockerfile { get; }

        public IDictionary<string, string> BuildArgs { get; }

        public string Context { get; }
    }
}
