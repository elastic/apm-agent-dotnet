// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using MongoDB.Driver.Core.Events;

namespace Elastic.Apm.MongoDb
{
	/// <inheritdoc cref="IEventSubscriber" />
	// ReSharper disable once UnusedMember.Global
	public class MongoDbEventSubscriber : IEventSubscriber
	{
		private readonly ReflectionEventSubscriber _subscriber;

		/// <summary>
		/// Creates instance of <see cref="MongoDbEventSubscriber" /> class.
		/// </summary>
		public MongoDbEventSubscriber() => _subscriber = new ReflectionEventSubscriber(new MongoListener());

		/// <summary>
		/// Tries to get an event handler for an event of type <typeparamref name="TEvent" />.
		/// </summary>
		/// <param name="handler">The handler.</param>
		/// <typeparam name="TEvent">The type of the event.</typeparam>
		/// <returns>
		/// <c>true</c> if this subscriber has provided an event handler; otherwise <c>false</c>.
		/// </returns>
		public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler) =>
			_subscriber.TryGetEventHandler(out handler);
	}
}
