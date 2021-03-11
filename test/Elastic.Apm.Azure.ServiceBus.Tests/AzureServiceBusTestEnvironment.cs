// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using Azure.Messaging.ServiceBus;
using Elastic.Apm.Tests.Utilities;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.ServiceBus.Tests
{
	/// <summary>
	/// A test environment for Azure Service Bus that deploys and configures an Azure Service Bus namespace
	/// in a given region and location
	/// </summary>
	public class AzureServiceBusTestEnvironment : IDisposable
	{
		private readonly TerraformResources _terraform;
		private readonly Dictionary<string, string> _variables;

		public AzureServiceBusTestEnvironment(IMessageSink messageSink)
		{
			var solutionRoot = SolutionPaths.Root;
			var terraformResourceDirectory = Path.Combine(solutionRoot, "build", "terraform", "azure", "service_bus");
			var credentials = AzureCredentials.Instance;

			_terraform = new TerraformResources(terraformResourceDirectory, credentials, messageSink);

			// TODO: source resource group and location from somewhere
			_variables = new Dictionary<string, string>
			{
				["location"] = "australiasoutheast",
				["resource_group"] = "russ-service-bus-test",
				["servicebus_namespace"] = "dotnet-" + Guid.NewGuid()
			};

			_terraform.Init();
			_terraform.Apply(_variables);

			ServiceBusConnectionString = _terraform.Output("connection_string");
			ServiceBusConnectionStringProperties = ServiceBusConnectionStringProperties.Parse(ServiceBusConnectionString);
		}

		public string ServiceBusConnectionString { get; }

		public ServiceBusConnectionStringProperties ServiceBusConnectionStringProperties { get; }

		public void Dispose() => _terraform.Destroy(_variables);
	}
}
