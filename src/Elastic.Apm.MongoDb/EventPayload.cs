namespace Elastic.Apm.MongoDb
{
	internal class EventPayload<T>
	{
		public EventPayload(T @event) => Event = @event;

		public T Event { get; }
	}
}
