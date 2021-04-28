using System.Collections.Generic;
using Docker.DotNet;
using Microsoft.Extensions.Logging;
using IP = System.Net.IPAddress;

namespace TestEnvironment.Docker.Containers.Postgres
{
    public class PostgresContainer : Container
    {
        private readonly string _userName;
        private readonly string _password;

        public PostgresContainer(
            DockerClient dockerClient,
            string name,
            string userName,
            string password,
            string imageName = "postgres",
            string tag = "latest",
            IDictionary<string, string> environmentVariables = null,
            IDictionary<ushort, ushort> ports = null,
            bool isDockerInDocker = false,
            bool reuseContainer = false,
            IContainerWaiter containerWaiter = null,
            IContainerCleaner containerCleaner = null,
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
                  containerWaiter,
                  containerCleaner,
                  logger)
        {
            _userName = userName;
            _password = password;
        }

        public string GetConnectionString() =>
            $"Host={(IsDockerInDocker ? IPAddress : IP.Loopback.ToString())};Port={(IsDockerInDocker ? 5432 : Ports[5432])};Username={_userName};Password={_password}";
    }
}
