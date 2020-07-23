// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
