using System.Collections.Generic;

namespace TestEnvironment.Docker.Containers.MariaDB
{
    public static class DockerEnvironmentBuilderExtensions
    {
        public static IDockerEnvironmentBuilder AddMariaDBContainer(this IDockerEnvironmentBuilder builder, string name, string rootPassword, string imageName = "mariadb", string tag = "latest", IDictionary<string, string> environmentVariables = null, IDictionary<ushort, ushort> ports = null, bool reuseContainer = false) =>
            builder.AddDependency(new MariaDBContainer(builder.DockerClient, name.GetContainerName(builder.EnvironmentName), rootPassword, imageName, tag, environmentVariables, ports, builder.IsDockerInDocker, reuseContainer, builder.Logger));
    }
}
