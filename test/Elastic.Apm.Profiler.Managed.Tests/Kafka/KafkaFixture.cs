// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Profiler.Managed.Tests.Kafka
{
	public class FixedKafkaTestcontainerConfiguration : KafkaTestcontainerConfiguration
	{
		private const string KafkaImage = "confluentinc/cp-kafka:6.0.1";

		private const string StartupScriptPath = "/testcontainers_start.sh";

		private const int KafkaPort = 9092;

		private const int BrokerPort = 9093;

		private const int ZookeeperPort = 2181;

		public override Func<IRunningDockerContainer, CancellationToken, Task> StartupCallback => (container, ct) =>
		{
			var writer = new StringWriter();
			writer.NewLine = "\n";
			writer.WriteLine("#!/bin/sh");
			writer.WriteLine($"echo 'clientPort={ZookeeperPort}' > zookeeper.properties");
			writer.WriteLine("echo 'dataDir=/var/lib/zookeeper/data' >> zookeeper.properties");
			writer.WriteLine("echo 'dataLogDir=/var/lib/zookeeper/log' >> zookeeper.properties");
			writer.WriteLine("zookeeper-server-start zookeeper.properties &");
			writer.WriteLine($"export KAFKA_ADVERTISED_LISTENERS='PLAINTEXT://{container.Hostname}:{container.GetMappedPublicPort(this.DefaultPort)},BROKER://localhost:{BrokerPort}'");
			writer.WriteLine(". /etc/confluent/docker/bash-config");
			writer.WriteLine("/etc/confluent/docker/configure");
			writer.WriteLine("/etc/confluent/docker/launch");
			return container.CopyFileAsync(StartupScriptPath, Encoding.UTF8.GetBytes(writer.ToString()), 0x1ff, ct: ct);
		};

	}

	[CollectionDefinition("Kafka")]
	public class KafkaCollection : ICollectionFixture<KafkaFixture>
	{
	}

	public class KafkaFixture : IAsyncLifetime
	{
		internal const int BrokerPort = 9093;

		private readonly KafkaTestcontainer _container;

		public KafkaFixture(IMessageSink messageSink)
		{
			var builder = new TestcontainersBuilder<KafkaTestcontainer>()
				.WithKafka(new FixedKafkaTestcontainerConfiguration());

			_container = builder.Build();
		}

		public async Task InitializeAsync()
		{
			await _container.StartAsync();

			// update advertised.listeners config value, which appears to be broken as it is not updated by the KafkaTestcontainerConfiguration
			var brokerAdvertisedListener = $"BROKER://localhost:{BrokerPort}";
			var plainTextListener = $"PLAINTEXT://{_container.Hostname}:{_container.Port}";
			var count = 0;
			ExecResult result = default;
			while (count < 10)
			{
				result = await _container.ExecAsync(new List<string>
					{
						"kafka-configs",
						"--alter",
						"--bootstrap-server",
						brokerAdvertisedListener,
						"--entity-type",
						"brokers",
						"--entity-name",
						"1",
						"--add-config",
						$"advertised.listeners=[{plainTextListener},{brokerAdvertisedListener}]"
					}
				);


				if (result.ExitCode == 0)
					break;

				await Task.Delay(1000);
				count++;
			}

			if (result.ExitCode != 0)
			{
				throw new InvalidOperationException(
					$"Updating kafka-configs returned exit code {result.ExitCode}.\nstdout: {result.Stdout}\nstderr:{result.Stderr}");
			}

			BootstrapServers = _container.BootstrapServers;
		}

		public async Task DisposeAsync() => await _container.DisposeAsync();

		public string BootstrapServers { get; private set; }
	}
}
