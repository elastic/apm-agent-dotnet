using System;
using System.Collections.Generic;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using IP = System.Net.IPAddress;

namespace TestEnvironment.Docker.Containers.Mongo
{
    public class MongoSingleReplicaSetContainer : Container, IMongoContainer
    {
        private readonly string _replicaSetName;
        private readonly ushort? _port;

        public MongoSingleReplicaSetContainer(
            DockerClient dockerClient,
            string name,
            string replicaSetName,
            string imageName,
            string tag = "latest",
            ushort? port = null,
            IDictionary<string, string> environmentVariables = null,
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
                port == null ? new Dictionary<ushort, ushort> { { 27017, 27017 } } : new Dictionary<ushort, ushort> { { port.Value, port.Value } },
                isDockerInDocker,
                reuseContainer,
                containerWaiter,
                containerCleaner,
                logger,
                port == null ? new List<string> { "/usr/bin/mongod", "--bind_ip_all", "--replSet", replicaSetName } : new List<string> { "/usr/bin/mongod", "--bind_ip_all", "--replSet", replicaSetName, "--port", port.Value.ToString() },
                new MongoSingleReplicaSetContainerInitializer(replicaSetName))
        {
            if (string.IsNullOrWhiteSpace(replicaSetName))
            {
                throw new ArgumentException("The value must be specified", nameof(replicaSetName));
            }

            _replicaSetName = replicaSetName;
            _port = port;
        }

        public string GetDirectNodeConnectionString()
        {
            var hostname = IsDockerInDocker ? IPAddress : IP.Loopback.ToString();
            var port = _port ?? (IsDockerInDocker ? 27017 : Ports[27017]);

            return $@"mongodb://{hostname}:{port}/?connect=direct";
        }

        public string GetConnectionString()
        {
            var hostname = IsDockerInDocker ? IPAddress : IP.Loopback.ToString();
            var port = _port ?? (IsDockerInDocker ? 27017 : Ports[27017]);

            return $@"mongodb://{hostname}:{port}/?replicaSet={_replicaSetName}";
        }

        protected override CreateContainerParameters GetCreateContainerParameters(string[] environmentVariables)
        {
            var createParams = base.GetCreateContainerParameters(environmentVariables);

            if (_port != null)
            {
                // custom mongo port isn't exposed by default from docker container
                createParams.ExposedPorts = new Dictionary<string, EmptyStruct> { { $"{_port.Value}/tcp", default } };
            }

            return createParams;
        }
    }
}
