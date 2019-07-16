using System;
using System.Collections.Generic;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	public interface IConfigurationReader
	{
		bool CaptureHeaders { get; }
		LogLevel LogLevel { get; }
		double MetricsIntervalInMillisecond { get; }
		string SecretToken { get; }
		IReadOnlyList<Uri> ServerUrls { get; }
		string ServiceName { get; }

		/// <summary>
		/// The agent limits stack trace collection to spans with durations equal or longer than the given value
		/// 0: Disables stack trace collection for spans completely
		/// negative value: stacktrace will be collected for all spans
		/// positive value n: stacktrace will be captured for spans with a duration equal or longer than n ms.
		/// </summary>
		double SpanFramesMinDurationInMilliseconds { get; }

		/// <summary>
		/// The number of stack frames the agent collects.
		/// 0: no stacktrace is collected
		/// negative: all frames must be collected
		/// positive number n: top n frames must be collected
		/// </summary>
		int StackTraceLimit { get; }

		double TransactionSampleRate { get; }
	}
}
