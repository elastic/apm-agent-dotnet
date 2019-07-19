using System;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Model
{
	internal class SpanCount
	{
		private int _started;
		private int _dropped;
		public int Started => _started;
		public int Dropped => _dropped;

		public void IncrementStarted() => Interlocked.Increment(ref _started);

		public void IncrementDropped() => Interlocked.Increment(ref _dropped);

		public override string ToString() => new ToStringBuilder(nameof(SpanCount)) { { "Started", Started }, { "Dropped", Dropped }, }.ToString();
	}
}
