using System.Collections.Generic;

namespace TestEnvironment.Docker.Containers.Postgres
{
    public static class DockerEnvironmentBuilderExtensions
    {
        public static IDockerEnvironmentBuilder AddPostgresContainer(
            this IDockerEnvironmentBuilder builder,
            string name,
            string userName = "root",
            string password = "securepass",
            string imageName = "postgres",
            string tag = "latest",
            IDictionary<string, string> environmentVariables = null,
            IDictionary<ushort, ushort> ports = null,
            bool reuseContainer = false)
        {
            return builder.AddDependency(
                new PostgresContainer(
                    builder.DockerClient,
                    name.GetContainerName(builder.EnvironmentName),
                    userName,
                    password,
                    imageName,
                    tag,
                    new Dictionary<string, string>
                    {
                        { "POSTGRES_USER", userName },
                        { "POSTGRES_PASSWORD", password }
                    }.MergeDictionaries(environmentVariables),
                    ports,
                    builder.IsDockerInDocker,
                    reuseContainer,
                    new PostgresContainerWaiter(builder.Logger),
                    new PostgresContainerCleaner(builder.Logger, userName),
                    builder.Logger));
        }
    }
}
