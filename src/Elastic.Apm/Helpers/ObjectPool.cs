using System;
using System.Threading;

namespace Elastic.Apm.Helpers
{
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
}
