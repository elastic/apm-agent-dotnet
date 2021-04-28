using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace TestEnvironment.Docker
{
    public class Container : IDependency
    {
        private readonly IContainerWaiter _containerWaiter;
        private readonly IContainerInitializer _containerInitializer;
        private readonly IContainerCleaner _containerCleaner;

        private readonly bool _reuseContainer;

        public Container(
            DockerClient dockerClient,
            string name,
            string imageName,
            string tag = "latest",
            IDictionary<string, string> environmentVariables = null,
            IDictionary<ushort, ushort> ports = null,
            bool isDockerInDocker = false,
            bool reuseContainer = false,
            IContainerWaiter containerWaiter = null,
            IContainerCleaner containerCleaner = null,
            ILogger logger = null,
            IList<string> entrypoint = null,
            IContainerInitializer containerInitializer = null)
        {
            Name = name;
            DockerClient = dockerClient;
            Logger = logger;
            IsDockerInDocker = isDockerInDocker;
            ImageName = imageName ?? throw new ArgumentNullException(nameof(imageName));
            Tag = tag;
            EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>();
            Ports = ports;
            _containerWaiter = containerWaiter;
            _containerInitializer = containerInitializer;
            _containerCleaner = containerCleaner;
            _reuseContainer = reuseContainer;

            Entrypoint = entrypoint;
        }

        public bool IsDockerInDocker { get; }

        public string Name { get; }

        public string Id { get; private set; }

        public string IPAddress { get; private set; }

        public IDictionary<ushort, ushort> Ports { get; private set; }

        public IList<string> Entrypoint { get; }

        public string ImageName { get; }

        public string Tag { get; }

        public IDictionary<string, string> EnvironmentVariables { get; }

        protected ILogger Logger { get; }

        protected DockerClient DockerClient { get; }

        public async Task Run(IDictionary<string, string> environmentVariables, CancellationToken token = default)
        {
            if (environmentVariables == null)
            {
                throw new ArgumentNullException(nameof(environmentVariables));
            }

            // Make sure that we don't try to add the same var twice.
            var stringifiedVariables = EnvironmentVariables.MergeDictionaries(environmentVariables)
                .Select(p => $"{p.Key}={p.Value}").ToArray();

            await RunContainerSafely(stringifiedVariables, token);

            if (_containerWaiter != null)
            {
                var isStarted = await _containerWaiter.Wait(this, token);
                if (!isStarted)
                {
                    Logger.LogError($"Container {Name} didn't start.");
                    throw new TimeoutException($"Container {Name} didn't start.");
                }
            }

            if (_containerInitializer != null)
            {
                await _containerInitializer.Initialize(this, token);
            }

            if (_reuseContainer && _containerCleaner != null)
            {
                await _containerCleaner.Cleanup(this, token);
            }
        }

        public Task Stop(CancellationToken token = default) =>
            DockerClient.Containers.StopContainerAsync(Id, new ContainerStopParameters { }, token);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!string.IsNullOrEmpty(Id))
                {
                    DockerClient.Containers.RemoveContainerAsync(Id, new ContainerRemoveParameters { Force = true })
                        .Wait();
                }
            }
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                if (!string.IsNullOrEmpty(Id))
                {
                    await DockerClient.Containers.RemoveContainerAsync(
                        Id,
                        new ContainerRemoveParameters { Force = true, RemoveVolumes = true });
                }
            }
        }

        protected virtual CreateContainerParameters GetCreateContainerParameters(string[] environmentVariables)
        {
            var createParams = new CreateContainerParameters
            {
                Name = Name,
                Image = $"{ImageName}:{Tag}",
                AttachStdout = true,
                Env = environmentVariables,
                Hostname = Name,
                HostConfig = new HostConfig
                {
                    PublishAllPorts = Ports == null
                }
            };

            if (Ports != null)
            {
                createParams.HostConfig.PortBindings = Ports
                    .ToDictionary(
                        p => $"{p.Key}/tcp",
                        p => (IList<PortBinding>)new List<PortBinding>
                            { new PortBinding { HostPort = p.Value.ToString() } });
            }

            if (Entrypoint != null && Entrypoint.Any())
            {
                createParams.Entrypoint = Entrypoint;
            }

            return createParams;
        }

        private async Task RunContainerSafely(string[] environmentVariables, CancellationToken token = default)
        {
            // Try to find container in docker session
            var startedContainer = await GetContainer(token);

            // If container already exist - remove that
            if (startedContainer != null)
            {
                // TODO: check status and network
                if (!_reuseContainer)
                {
                    await DockerClient.Containers.RemoveContainerAsync(
                        startedContainer.ID,
                        new ContainerRemoveParameters { Force = true },
                        token);
                    startedContainer = await CreateContainer(environmentVariables, token);
                }
            }
            else
            {
                startedContainer = await CreateContainer(environmentVariables, token);
            }

            Logger.LogInformation($"Container '{Name}' has been run.");
            Logger.LogDebug($"Container state: {startedContainer.State}");
            Logger.LogDebug($"Container status: {startedContainer.Status}");
            Logger.LogDebug(
                $"Container IPAddress: {startedContainer.NetworkSettings.Networks.FirstOrDefault().Key} - {startedContainer.NetworkSettings.Networks.FirstOrDefault().Value.IPAddress}");

            Id = startedContainer.ID;
            IPAddress = startedContainer.NetworkSettings.Networks.FirstOrDefault().Value.IPAddress;
            Ports = startedContainer.Ports.ToDictionary(p => p.PrivatePort, p => p.PublicPort);
        }

        private async Task<ContainerListResponse> CreateContainer(string[] environmentVariables, CancellationToken token)
        {
            // Create new container
            var createParams = GetCreateContainerParameters(environmentVariables);

            var container = await DockerClient.Containers.CreateContainerAsync(createParams, token);

            // Run container
            await DockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters(), token);

            // Try to find container in docker session
            return await GetContainer(token);
        }

        private async Task<ContainerListResponse> GetContainer(CancellationToken token)
        {
            var containerName = $"/{Name}";

            var containers = await DockerClient.Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    All = true,
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                        { ["name"] = new Dictionary<string, bool> { [containerName] = true } }
                }, token);

            return containers.FirstOrDefault(x => x.Names.Contains(containerName));
        }
    }
}
