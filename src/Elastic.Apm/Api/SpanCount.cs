// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Api
{
	public class SpanCount
	{
		private int _dropped;
		private int _started;
		private int _total;

		/// <summary>
		/// Number of spans that have been dropped by the agent recording the transaction
		/// </summary>
		public int Dropped => _dropped;

		/// <summary>
		/// Number of correlated spans that are recorded
		/// </summary>
		[Required]
		public int Started => _started;

		internal void IncrementStarted() => Interlocked.Increment(ref _started);

		internal void IncrementDropped() => Interlocked.Increment(ref _dropped);

		/// <summary>
		/// Moves <paramref name="count" /> spans that were started but then discarded instead of being reported
		/// (see <c>exit_span_min_duration</c>) from <see cref="Started" /> to <see cref="Dropped" />, so that
		/// <see cref="Started" /> keeps reflecting the number of spans that are actually recorded.
		/// </summary>
		internal void MoveStartedToDropped(int count)
		{
			Interlocked.Add(ref _started, -count);
			Interlocked.Add(ref _dropped, count);
		}

		internal int IncrementTotal() => Interlocked.Increment(ref _total);

		public override string ToString() =>
			new ToStringBuilder(nameof(SpanCount)) { { nameof(Started), Started }, { nameof(Dropped), Dropped } }.ToString();
	}
}
