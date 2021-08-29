// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Model;

namespace Elastic.Apm.Azure.Storage
{
	/// <summary>
	/// Subscribes to Azure.Core events to set the url tag on parent activities.
	/// </summary>
	internal class AzureCoreDiagnosticListener : IDiagnosticListener
	{
		private readonly IApmAgent _agent;

		public AzureCoreDiagnosticListener(IApmAgent agent) => _agent = agent;

		public string Name => "Azure.Core";

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(KeyValuePair<string, object> value)
		{
			if (value.Key == "Azure.Core.Http.Request.Start")
			{
				if (_agent.GetCurrentExecutionSegment() != null)
				{
					var currentActivity = Activity.Current;
					if (currentActivity != null)
					{
						var targetActivity = FindTargetActivity(currentActivity);
						if (targetActivity != null && string.IsNullOrEmpty(targetActivity.Tags.FirstOrDefault(t => t.Key == "url").Value))
						{
							var url = currentActivity.Tags.FirstOrDefault(t => t.Key == "http.url").Value;
							if (!string.IsNullOrEmpty(url))
								targetActivity.SetTag("url", url);
						}
					}
				}
			}
		}

		/// <summary>
		/// Finds the target Azure activity to set the url tag on.
		/// Some Azure SDK versions and methods fire two activities with the same operation name for the same operation e.g.
		///
		/// QueueClient.ReceiveMessage activity in package version 12.7.0 is started twice for QueueClient.ReceiveMessageAsync():
		/// https://github.com/Azure/azure-sdk-for-net/blob/b0403de6fff3cfc961deba543e3c657287fc0626/sdk/storage/Azure.Storage.Queues/src/QueueClient.cs#L2343-L2347
		/// and in the internal method call ReceiveMessagesInternal
		/// https://github.com/Azure/azure-sdk-for-net/blob/b0403de6fff3cfc961deba543e3c657287fc0626/sdk/storage/Azure.Storage.Queues/src/QueueClient.cs#L2180-L2183
		///
		/// With regards to Elastic APM, we are only interested in the first QueueClient.ReceiveMessage activity, for which we will start a
		/// transaction and start an APM transaction Activity. In this case, we end up with the following Activity chain:
		///
		/// QueueClient.ReceiveMessage activity (the activity we're interested in and want to set url tag on)
		/// Apm Transaction activity
		/// QueueClient.ReceiveMessage activity
		/// Azure.Core.Http.Request activity (which contains the http.url tag with the url)
		///
		/// For other Azure SDK versions and methods, only a single activity might be started. This is the majority case and we will start a
		/// span when this single activity is started. This ends up with the following Activity chain:
		///
		///
		/// </summary>
		/// <param name="activity"></param>
		/// <returns></returns>
		private static Activity FindTargetActivity(Activity activity)
		{
			var parentActivity = activity.Parent;
			Activity transactionActivity = null;

			while (parentActivity != null)
			{
				if (parentActivity.OperationName == Transaction.ApmTransactionActivityName)
				{
					transactionActivity = parentActivity;
					break;
				}

				parentActivity = parentActivity.Parent;
			}

			return transactionActivity?.Parent ?? activity.Parent;
		}
	}
}
