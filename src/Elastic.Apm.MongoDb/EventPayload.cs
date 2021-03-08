namespace Elastic.Apm.Mongo
{
	internal class EventPayload<T>
	{
		public EventPayload(T @event) => Event = @event;

		public T Event { get; }
	}
}
