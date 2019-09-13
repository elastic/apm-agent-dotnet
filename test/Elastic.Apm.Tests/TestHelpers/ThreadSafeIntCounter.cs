using System.Threading;

namespace Elastic.Apm.Tests.TestHelpers
{
	internal class ThreadSafeIntCounter
	{
		internal ThreadSafeIntCounter(int initialValue = 0) => _value = initialValue;

		private volatile int _value;

		internal int Value => _value;

		internal int Increment() => Interlocked.Increment(ref _value);
	}
}
