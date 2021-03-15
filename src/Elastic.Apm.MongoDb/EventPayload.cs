// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.MongoDb
{
	internal class EventPayload<T>
	{
		public EventPayload(T @event) => Event = @event;

		public T Event { get; }
	}
}
