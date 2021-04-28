using System.Collections.Generic;

namespace TestEnvironment.Docker.Containers.Mail
{
    public static class DockerEnvironmentBuilderExtensions
    {
        public static IDockerEnvironmentBuilder AddMailContainer(
            this IDockerEnvironmentBuilder builder,
            string name,
            string imageName = "mailhog/mailhog",
            string tag = "latest",
            ushort smptPort = 1025,
            ushort apiPort = 8025,
            string deleteEndpoint = "api/v1/messages",
            IDictionary<string, string> environmentVariables = null,
            IDictionary<ushort, ushort> ports = null,
            bool reuseContainer = false) =>
            builder.AddDependency(new MailContainer(builder.DockerClient, name.GetContainerName(builder.EnvironmentName), imageName, tag, smptPort, apiPort, deleteEndpoint, environmentVariables, ports, builder.IsDockerInDocker, reuseContainer, builder.Logger));
    }
}
