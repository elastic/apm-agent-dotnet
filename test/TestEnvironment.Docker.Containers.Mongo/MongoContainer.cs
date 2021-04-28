using System.Collections.Generic;
using Docker.DotNet;
using Microsoft.Extensions.Logging;
using IP = System.Net.IPAddress;

namespace TestEnvironment.Docker.Containers.Mongo
{
    public class MongoContainer : Container, IMongoContainer
    {
        private readonly string _userName;
        private readonly string _userPassword;

        public MongoContainer(
            DockerClient dockerClient,
            string name,
            string userName,
            string userPassword,
            string imageName,
            string tag = "latest",
            IDictionary<string, string> environmentVariables = null,
            IDictionary<ushort, ushort> ports = null,
            bool isDockerInDocker = false,
            bool reuseContainer = false,
            IContainerWaiter containerWaiter = null,
            IContainerCleaner containerCleaner = null,
            ILogger logger = null)
            : base(dockerClient, name, imageName, tag, environmentVariables, ports, isDockerInDocker, reuseContainer, containerWaiter, containerCleaner, logger)
        {
            _userName = userName;
            _userPassword = userPassword;
        }

        public string GetConnectionString()
        {
            var hostname = IsDockerInDocker ? IPAddress : IP.Loopback.ToString();
            var port = IsDockerInDocker ? 27017 : Ports[27017];

            return string.IsNullOrEmpty(_userName) || string.IsNullOrEmpty(_userPassword)
                ? $@"mongodb://{hostname}:{port}"
                : $@"mongodb://{_userName}:{_userPassword}@{hostname}:{port}";
        }
    }
}