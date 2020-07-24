// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Model
{
	internal class SpanCount
	{
		private int _dropped;
		private int _started;
		private int _total;
		public int Dropped => _dropped;
		public int Started => _started;

		public void IncrementStarted() => Interlocked.Increment(ref _started);

		public void IncrementDropped() => Interlocked.Increment(ref _dropped);

		public int IncrementTotal() => Interlocked.Increment(ref _total);

		public override string ToString() =>
			new ToStringBuilder(nameof(SpanCount)) { { nameof(Started), Started }, { nameof(Dropped), Dropped } }.ToString();
	}
}
