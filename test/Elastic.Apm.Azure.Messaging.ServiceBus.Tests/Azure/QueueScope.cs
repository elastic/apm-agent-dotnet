// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;

namespace Elastic.Apm.Azure.Messaging.ServiceBus.Tests.Azure
{
	public class QueueScope : IAsyncDisposable
	{
		public string QueueName { get; }
		private readonly QueueProperties _properties;
		private readonly ServiceBusAdministrationClient _adminClient;

		private QueueScope(ServiceBusAdministrationClient adminClient, string queueName, QueueProperties properties)
		{
			_adminClient = adminClient;
			QueueName = queueName;
			_properties = properties;
		}
		
		public static async Task<QueueScope> CreateWithQueue(ServiceBusAdministrationClient adminClient)
		{
			var queueName = Guid.NewGuid().ToString("D");
			var response = await adminClient.CreateQueueAsync(queueName).ConfigureAwait(false);
			return new QueueScope(adminClient, queueName, response.Value);
		}

		public async ValueTask DisposeAsync() => 
			await _adminClient.DeleteQueueAsync(QueueName).ConfigureAwait(false);
	}
}
