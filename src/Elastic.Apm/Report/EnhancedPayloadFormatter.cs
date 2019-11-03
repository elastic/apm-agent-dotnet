using System;
using System.Text;
using System.Threading;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Model;
using Elastic.Apm.Report.Serialization;

namespace Elastic.Apm.Report
{
	internal class StringBuilderPool : ObjectPool<StringBuilder>
	{
		internal StringBuilderPool(int amount, int initialCharactersAmount, int charactersLimit)
			: base(amount,
				() => new StringBuilder(initialCharactersAmount),
				builder =>
				{
					builder.Length = 0;
					if (builder.Capacity > charactersLimit) builder.Capacity = charactersLimit;
				})
		{
		}
	}

	internal class ObjectPool<T> where T : class
	{
		private readonly T[] _objects;
		private readonly Action<T> _returnAction;

		private int _index = -1;

		internal ObjectPool(int amount, Func<T> valueFactory, Action<T> returnAction)
		{
			if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
			if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));
			if (returnAction == null) throw new ArgumentNullException(nameof(returnAction));

			_objects = new T[amount];
			for (var i = 0; i < amount; i++) _objects[i] = valueFactory();

			_returnAction = returnAction;
		}

		public ObjectHolder Get()
		{
			int index;
			T @object;

			do
				index = Interlocked.Increment(ref _index) % _objects.Length;
			while ((@object = Interlocked.Exchange(ref _objects[index], null)) == null);

			return new ObjectHolder(this, @object, index);
		}

		private void ReturnToPool(T @object, int index)
		{
			_returnAction(@object);
			_objects[index] = @object;
		}

		internal struct ObjectHolder : IDisposable
		{
			internal T Object { get; }

			private readonly ObjectPool<T> _pool;
			private readonly int _index;

			public ObjectHolder(ObjectPool<T> pool, T @object, int index)
			{
				_pool = pool;
				Object = @object;
				_index = index;
			}

			public void Dispose() => _pool.ReturnToPool(Object, _index);
		}
	}

	internal class EnhancedPayloadFormatter : IPayloadFormatter
	{
		private readonly IApmLogger _logger;
		private readonly PayloadItemSerializer _payloadItemSerializer;
		private readonly Metadata _metadata;

		private string _cachedMetadataJsonLine;

		private readonly StringBuilderPool _stringBuilderPool;

		public EnhancedPayloadFormatter(IApmLogger logger, IConfigurationReader config, Metadata metadata)
		{
			_logger = logger.Scoped(nameof(EnhancedPayloadFormatter));
			_payloadItemSerializer = new PayloadItemSerializer(config);
			_metadata = metadata;

			_stringBuilderPool = new StringBuilderPool(5, 1_000, 20_000);
		}

		public string FormatPayload(object[] items)
		{
			using (var holder = _stringBuilderPool.Get())
			{
				var ndjson = holder.Object;
				if (_cachedMetadataJsonLine == null)
					_cachedMetadataJsonLine = "{\"metadata\": " + _payloadItemSerializer.SerializeObject(_metadata) + "}";
				ndjson.AppendLine(_cachedMetadataJsonLine);

				foreach (var item in items)
				{
					var serialized = _payloadItemSerializer.SerializeObject(item);
					switch (item)
					{
						case Transaction _:
							ndjson.Append("{\"transaction\": ");
							ndjson.Append(serialized);
							ndjson.AppendLine("}");
							break;
						case Span _:
							ndjson.Append("{\"span\": ");
							ndjson.Append(serialized);
							ndjson.AppendLine("}");
							break;
						case Error _:
							ndjson.Append("{\"error\": ");
							ndjson.Append(serialized);
							ndjson.AppendLine("}");
							break;
						case MetricSet _:
							ndjson.Append("{\"metricset\": ");
							ndjson.Append(serialized);
							ndjson.AppendLine("}");
							break;
					}

					_logger?.Trace()?.Log("Serialized item to send: {ItemToSend} as {SerializedItem}", item, serialized);
				}

				return ndjson.ToString();
			}
		}
	}
}
