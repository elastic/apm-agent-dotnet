using System.Collections.Generic;

namespace TestEnvironment.Docker.Containers.Ftp
{
    public static class DockerEnvironmentBuilderExtensions
    {
        public static IDockerEnvironmentBuilder AddFtpContainer(
            this IDockerEnvironmentBuilder builder,
            string name,
            string ftpUserName,
            string ftpPassword,
            string imageName = "stilliard/pure-ftpd",
            string tag = "hardened",
            IDictionary<string, string> environmentVariables = null,
            IDictionary<ushort, ushort> ports = null,
            bool reuseContainer = false) =>
            builder.AddDependency(new FtpContainer(builder.DockerClient, name.GetContainerName(builder.EnvironmentName), ftpUserName, ftpPassword, imageName, tag, environmentVariables, ports, builder.IsDockerInDocker, reuseContainer, builder.Logger));
    }
}
