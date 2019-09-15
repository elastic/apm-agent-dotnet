using System.Threading;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class ThreadSafeLongCounter
	{
		internal ThreadSafeLongCounter(long initialValue = 0) => _value = initialValue;

		private long _value;

		internal long Value => Volatile.Read(ref _value);

		internal long Increment() => Interlocked.Increment(ref _value);
	}
}
