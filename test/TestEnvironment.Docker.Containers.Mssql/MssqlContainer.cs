using System.Collections.Generic;
using Docker.DotNet;
using Microsoft.Extensions.Logging;
using IP = System.Net.IPAddress;

namespace TestEnvironment.Docker.Containers.Mssql
{
    public sealed class MssqlContainer : Container
    {
        private readonly string _saPassword;

        public MssqlContainer(
            DockerClient dockerClient,
            string name,
            string saPassword,
            string imageName = "microsoft/mssql-server-linux",
            string tag = "latest",
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
                  new Dictionary<string, string> { ["ACCEPT_EULA"] = "Y", ["SA_PASSWORD"] = saPassword, ["MSSQL_PID"] = "Express" }.MergeDictionaries(environmentVariables),
                  ports,
                  isDockerInDocker,
                  reuseContainer,
                  new MssqlContainerWaiter(logger),
                  new MssqlContainerCleaner(logger),
                  logger)
        {
            _saPassword = saPassword;
        }

        public string GetConnectionString() =>
            $"Data Source={(IsDockerInDocker ? IPAddress : IP.Loopback.ToString())}, {(IsDockerInDocker ? 1433 : Ports[1433])}; UID=sa; pwd={_saPassword};";
    }
}
