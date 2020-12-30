// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;

namespace Elastic.Apm.Tests.Utilities
{
	internal class ThreadSafeIntCounter
	{
		internal ThreadSafeIntCounter(int initialValue = 0) => _value = initialValue;

		private volatile int _value;

		internal int Value => _value;

		internal int Increment() => Interlocked.Increment(ref _value);
	}
}
