// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;

namespace Elastic.Apm.Azure.ServiceBus.Tests
{
	public class TopicScope : IAsyncDisposable
	{
		public string TopicName { get; }
		private readonly TopicProperties _properties;
		private readonly ServiceBusAdministrationClient _adminClient;

		private TopicScope(ServiceBusAdministrationClient adminClient, string queueName, TopicProperties properties)
		{
			_adminClient = adminClient;
			TopicName = queueName;
			_properties = properties;
		}
		
		public static async Task<TopicScope> CreateWithTopic(ServiceBusAdministrationClient adminClient)
		{
			var topicName = Guid.NewGuid().ToString("D");
			var response = await adminClient.CreateTopicAsync(topicName).ConfigureAwait(false);
			return new TopicScope(adminClient, topicName, response.Value);
		}

		public async ValueTask DisposeAsync() => 
			await _adminClient.DeleteQueueAsync(TopicName).ConfigureAwait(false);
	}
}
