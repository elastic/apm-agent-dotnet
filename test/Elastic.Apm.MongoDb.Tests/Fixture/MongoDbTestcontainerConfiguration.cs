// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Elastic.Apm.MongoDb.Tests.Fixture
{
	public class MongoDbTestcontainerConfiguration : TestcontainerDatabaseConfiguration
	{
		private const int MongoDbPort = 27017;
		private const string MongoDbImage = "mongo:4.4.5";

		private readonly MemoryStream _stdout = new MemoryStream();
		private readonly MemoryStream _stderr = new MemoryStream();

		public MongoDbTestcontainerConfiguration() : this(MongoDbImage) { }

		public MongoDbTestcontainerConfiguration(string image) : base(image, MongoDbPort)
		{
			OutputConsumer = Consume.RedirectStdoutAndStderrToStream(_stdout, _stderr);
			WaitStrategy = Wait.ForUnixContainer().UntilMessageIsLogged(OutputConsumer.Stdout, "Waiting for connections");
		}

		public override string Username
		{
			get => Environments.TryGetValue("MONGO_INITDB_ROOT_USERNAME", out var username) ? username : null;
			set => Environments["MONGO_INITDB_ROOT_USERNAME"] = value;
		}

		public override string Password
		{
			get => Environments.TryGetValue("MONGO_INITDB_ROOT_PASSWORD", out var password) ? password : null;
			set => Environments["MONGO_INITDB_ROOT_PASSWORD"] = value;
		}

		public override IOutputConsumer OutputConsumer { get; }

		public override IWaitForContainerOS WaitStrategy { get; }
	}
}
