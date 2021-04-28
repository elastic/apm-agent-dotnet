// Based on the elastic-apm-mongo project by Vadim Hatsura (@vhatsura)
// https://github.com/vhatsura/elastic-apm-mongo
// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Elastic.Apm.MongoDb
{
	internal class EventPayload<T>
	{
		public EventPayload(T @event) => Event = @event;

		public T Event { get; }
	}
}
