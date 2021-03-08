using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TestEnvironment.Docker;

namespace Elastic.Apm.Mongo.IntegrationTests.Fixture.TestEnvironment.Docker
{
	// TestEnvironment.Docker.Containers.Mongo has dependency on MongoDB.Driver of 2.8.0 version
	public static class DockerEnvironmentBuilderExtensions
	{
		public static IDockerEnvironmentBuilder AddMongoContainer(
			this IDockerEnvironmentBuilder builder,
			string name,
			string userName = "root",
			string userPassword = "example",
			string imageName = "mongo",
			string tag = "latest",
			IDictionary<string, string> environmentVariables = null,
			IDictionary<ushort, ushort> ports = null,
			bool reuseContainer = false) =>
			builder.AddDependency(
				new MongoContainer(
					builder.DockerClient,
					name.GetContainerName(builder.EnvironmentName),
					userName,
					userPassword,
					imageName,
					tag,
					new Dictionary<string, string>
					{
						{ "MONGO_INITDB_ROOT_USERNAME", userName },
						{ "MONGO_INITDB_ROOT_PASSWORD", userPassword }
					}.MergeDictionaries(environmentVariables),
					ports,
					builder.IsDockerInDocker,
					reuseContainer,
					new MongoContainerWaiter(builder.Logger),
					new MongoContainerCleaner(builder.Logger),
					builder.Logger));
	}
}
