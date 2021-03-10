// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Azure.Messaging.ServiceBus;

namespace Elastic.Apm.Azure.ServiceBus.Tests
{
	public class AzureServiceBusTestEnvironment
	{
		public AzureServiceBusTestEnvironment()
		{
			var serviceBusConnectionString = Environment.GetEnvironmentVariable("AZURE_SERVICE_BUS_CONNECTION_STRING");
			if (string.IsNullOrEmpty(serviceBusConnectionString))
			{
				throw new ArgumentException(
					"connection string for Azure Service Bus required. A connection string can be passed with AZURE_SERVICE_BUS_CONNECTION_STRING environment variable");
			}

			ServiceBusConnectionString = serviceBusConnectionString;
			ServiceBusConnectionStringProperties = ServiceBusConnectionStringProperties.Parse(serviceBusConnectionString);
		}

		public string ServiceBusConnectionString { get; }

		public ServiceBusConnectionStringProperties ServiceBusConnectionStringProperties { get; }
	}
}
