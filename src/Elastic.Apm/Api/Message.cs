// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Api.Constraints;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Holds details related to message receiving and publishing if the captured event integrates with a messaging system
	/// </summary>
	public class Message
	{
		/// <summary>
		/// Body of the received message
		/// </summary>
		public string Body { get; set; }

		/// <summary>
		/// Headers received with the message
		/// </summary>
		public Dictionary<string, string> Headers { get; set; }

		/// <inheritdoc cref="MessageAge"/>
		public MessageAge Age { get; set; }

		/// <see cref="MessageQueue"/>
		public MessageQueue Queue { get; set; }

		/// <summary>
		/// optional routing key of the received message as set on the queuing system, such as in RabbitMQ.
		/// </summary>
		public string RoutingKey { get; set; }
	}

	/// <summary>
	/// Age of the message. If the monitored messaging framework provides a timestamp for the message, agents may use it.
	/// Otherwise, the sending agent can add a timestamp in milliseconds since the Unix epoch to the message's metadata to be retrieved by the
	/// receiving agent. If a timestamp is not available, agents should omit this field.
	/// </summary>
	public class MessageAge
	{
		/// <summary>
		/// Age of the message in milliseconds.
		/// </summary>
		public long Ms { get; set; }
	}

	/// <summary>
	/// Information about the message queue where the message is received.
	/// </summary>
	public class MessageQueue
	{
		/// <summary>
		/// Name of the message queue where the message is received
		/// </summary>
		[MaxLength]
		public string Name { get; set; }
	}
}
