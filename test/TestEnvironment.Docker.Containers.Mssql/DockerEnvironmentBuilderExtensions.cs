using System.Collections.Generic;

namespace TestEnvironment.Docker.Containers.Mssql
{
    public static class DockerEnvironmentBuilderExtensions
    {
        public static IDockerEnvironmentBuilder AddMssqlContainer(this IDockerEnvironmentBuilder builder, string name, string saPassword, string imageName = "mcr.microsoft.com/mssql/server", string tag = "latest", IDictionary<string, string> environmentVariables = null, IDictionary<ushort, ushort> ports = null, bool reuseContainer = false) =>
            builder.AddDependency(new MssqlContainer(builder.DockerClient, name.GetContainerName(builder.EnvironmentName), saPassword, imageName, tag, environmentVariables, ports, builder.IsDockerInDocker, reuseContainer, builder.Logger));
    }
}
