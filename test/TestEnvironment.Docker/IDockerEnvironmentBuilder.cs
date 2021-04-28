using System.Collections.Generic;
using System.IO;
using Docker.DotNet;
using Microsoft.Extensions.Logging;

namespace TestEnvironment.Docker
{
    public interface IDockerEnvironmentBuilder
    {
        DockerClient DockerClient { get; }

        ILogger Logger { get; }

        bool IsDockerInDocker { get; }

        string EnvironmentName { get; }

        IDockerEnvironmentBuilder DockerInDocker(bool dockerInDocker = true);

        IDockerEnvironmentBuilder UseDefaultNetwork();

        IDockerEnvironmentBuilder WithLogger(ILogger logger);

        IDockerEnvironmentBuilder IgnoreFolders(params string[] ignoredFolders);

        IDockerEnvironmentBuilder SetName(string environmentName);

        IDockerEnvironmentBuilder SetVariable(IDictionary<string, string> variables);

        IDockerEnvironmentBuilder AddDependency(IDependency dependency);

        IDockerEnvironmentBuilder AddContainer(string name, string imageName, string tag = "latest", IDictionary<string, string> environmentVariables = null, IDictionary<ushort, ushort> ports = null, bool reuseContainer = false, IContainerWaiter containerWaiter = null, IContainerCleaner containerCleaner = null);

        IDockerEnvironmentBuilder AddFromCompose(Stream composeFileStream);

        IDockerEnvironmentBuilder AddFromDockerfile(string name, string dockerfile, IDictionary<string, string> buildArgs = null, string context = ".", IDictionary<string, string> environmentVariables = null, IDictionary<ushort, ushort> ports = null, bool reuseContainer = false, IContainerWaiter containerWaiter = null, IContainerCleaner containerCleaner = null);

        DockerEnvironment Build();
    }
}
