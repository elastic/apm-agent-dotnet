using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Elastic.Apm.Helpers
{
	internal class AgentTimer : IAgentTimer
	{
		private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

		internal AgentTimer() => WhenStarted = new AgentTimeInstant(this, TimeSpan.Zero);

		public AgentTimeInstant Now => new AgentTimeInstant(this, _stopwatch.Elapsed);

		internal AgentTimeInstant WhenStarted { get; }

		public Task Delay(AgentTimeInstant until, CancellationToken cancellationToken = default)
		{
			if (!until.IsCompatibleWith(this))
			{
				throw new ArgumentOutOfRangeException(nameof(until)
					, $"{nameof(until)} argument time instant should have this Agent timer as its source. {nameof(until)}: {until}");
			}

			return DelayAsyncImpl(until, cancellationToken);
		}

		private async Task DelayAsyncImpl(AgentTimeInstant until, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var now = Now;
			var delayRemainder = until - now;
			if (delayRemainder <= TimeSpan.Zero) return;

			await Task.Delay(delayRemainder, cancellationToken);
		}
	}
}
