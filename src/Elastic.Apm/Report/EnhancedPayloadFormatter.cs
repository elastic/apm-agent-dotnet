using System;
using System.IO;
using System.Text;
using System.Threading;
using Elastic.Apm.Config;
using Elastic.Apm.Metrics;
using Elastic.Apm.Model;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Report
{
	internal class StringWriterPool : ObjectPool<StringWriter>
	{
		internal StringWriterPool(int amount, int initialCharactersAmount, int charactersLimit)
			: base(amount,
				() => new StringWriter(new StringBuilder(initialCharactersAmount)),
				writer =>
				{
					var builder = writer.GetStringBuilder();
					builder.Length = 0;
					if (builder.Capacity > charactersLimit) builder.Capacity = charactersLimit;
				}) { }
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
		private readonly Metadata _metadata;

		private string _cachedMetadataJsonLine;

		private readonly StringWriterPool _stringWriterPool;

		internal JsonSerializerSettings Settings { get; }
		private readonly JsonSerializer _jsonSerializer;

		public EnhancedPayloadFormatter(IConfigurationReader config, Metadata metadata)
		{
			_metadata = metadata;

			_stringWriterPool = new StringWriterPool(5, 1_000, 20_000);

			Settings = new JsonSerializerSettings
			{
				ContractResolver = new ElasticApmContractResolver(config),
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.None,
			};
			_jsonSerializer = JsonSerializer.CreateDefault(Settings);
		}

		public string FormatPayload(object[] items)
		{
			static void WriteItem(string name, object item, StringWriter writer, JsonSerializer jsonSerializer)
			{
				writer.Write($"{{\"{name}\":");

				using (var jsonWriter = new JsonTextWriter(writer) { CloseOutput = false })
					jsonSerializer.Serialize(jsonWriter, item);

				writer.WriteLine("}");
			}

			if (_cachedMetadataJsonLine == null)
				_cachedMetadataJsonLine = "{\"metadata\":" + JsonConvert.SerializeObject(_metadata, Settings) + "}";

			using (var holder = _stringWriterPool.Get())
			{
				var writer = holder.Object;

				writer.WriteLine(_cachedMetadataJsonLine);

				foreach (var item in items)
				{
					switch (item)
					{
						case Transaction _:
							WriteItem("transaction", item, writer, _jsonSerializer);
							break;
						case Span _:
							WriteItem("span", item, writer, _jsonSerializer);
							break;
						case Error _:
							WriteItem("error", item, writer, _jsonSerializer);
							break;
						case MetricSet _:
							WriteItem("metricset", item, writer, _jsonSerializer);
							break;
					}
				}

				return writer.ToString();
			}
		}
	}
}
