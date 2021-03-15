// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;

namespace Elastic.Apm.Azure.Messaging.ServiceBus.Tests.Azure
{
	public class TopicScope : IAsyncDisposable
	{
		private readonly ServiceBusAdministrationClient _adminClient;
		public string TopicName { get; }
		public string SubscriptionName { get; }

		private TopicScope(ServiceBusAdministrationClient adminClient, string topicName, string subscriptionName)
		{
			_adminClient = adminClient;
			TopicName = topicName;
			SubscriptionName = subscriptionName;
		}

		public static async Task<TopicScope> CreateWithTopic(ServiceBusAdministrationClient adminClient)
		{
			var topicName = Guid.NewGuid().ToString("D");
			var response = await adminClient.CreateTopicAsync(topicName).ConfigureAwait(false);
			return new TopicScope(adminClient, topicName, null);
		}

		public static async Task<TopicScope> CreateWithTopicAndSubscription(ServiceBusAdministrationClient adminClient)
		{
			var topicName = Guid.NewGuid().ToString("D");
			var subscriptionName = Guid.NewGuid().ToString("D");
			var topicResponse = await adminClient.CreateTopicAsync(topicName).ConfigureAwait(false);
			var subscriptionResponse =
				await adminClient.CreateSubscriptionAsync(topicName, subscriptionName).ConfigureAwait(false);
			return new TopicScope(adminClient, topicName, subscriptionName);
		}


		public async ValueTask DisposeAsync() =>
			await _adminClient.DeleteQueueAsync(TopicName).ConfigureAwait(false);
	}
}
