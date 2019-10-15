using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	public interface IConfigurationReader
	{
		string CaptureBody { get; }
		List<string> CaptureBodyContentTypes { get; }
		bool CaptureHeaders { get; }
		bool CentralConfig { get; }
		string Environment { get; }

		/// <summary>
		/// The maximal amount of time (in seconds) events are held in queue until there is enough to send a batch.
		/// It's possible for a batch to contain less then <seealso cref="MaxBatchEventCount" /> events
		/// if there are events that need to be sent out because they were held for too long.
		/// A lower value will increase the load on your APM server,
		/// while a higher value can increase the memory pressure on your app.
		/// A higher value also impacts the time until transactions are indexed and searchable in Elasticsearch.
		/// <list type="bullet">
		/// 	<item>
		/// 		<description>
		/// 			Positive number - The maximal amount of time to hold events in queue.
		/// 		</description>
		/// 	</item>
		/// 	<item>
		/// 		<description>
		/// 			0 - Events are not held in queue but are sent immediately.
		/// 		</description>
		/// 	</item>
		/// 	<item>
		/// 		<description>
		/// 			Negative - Invalid and the default value is used instead.
		/// 		</description>
		/// 	</item>
		/// </list>
		/// Default value: <seealso cref="ConfigConsts.DefaultValues.FlushIntervalInMilliseconds" />
		/// </summary>
		TimeSpan FlushInterval { get; }

		LogLevel LogLevel { get; }

		/// <summary>
		/// The maximal number of events to send in a batch.
		/// It's possible for a batch contain less then the maximum events
		/// if there are events that need to be sent out because they were held for too long.
		/// <list type="bullet">
		/// 	<item>
		/// 		<description>
		/// 			Positive number - The maximal number of of events to send in a batch.
		/// 		</description>
		/// 	</item>
		/// 	<item>
		/// 		<description>
		/// 			0  and negative - Invalid and the default value is used instead.
		/// 		</description>
		/// 	</item>
		/// </list>
		/// Default value: <seealso cref="ConfigConsts.DefaultValues.MaxBatchEventCount" />
		/// Also see: <seealso cref="FlushInterval" /> and <seealso cref="MaxQueueEventCount" />
		/// </summary>
		int MaxBatchEventCount { get; }

		/// <summary>
		/// The maximal number of events to hold in queue as candidates to be sent.
		/// If the queue is at its maximum capacity then the agent discards the new events
		/// until the queue has free space.
		/// <list type="bullet">
		/// 	<item>
		/// 		<description>
		/// 			Positive number - The maximal number of of events to send in a batch.
		/// 				If <c>MaxQueueEventCount</c> is less than <seealso cref="MaxBatchEventCount" /> then
		/// <seealso cref="MaxBatchEventCount" /> is used as <c>MaxQueueEventCount</c>.
		/// 		</description>
		/// 	</item>
		/// 	<item>
		/// 		<description>
		/// 			0  and negative - Invalid and the default value is used instead.
		/// 		</description>
		/// 	</item>
		/// </list>
		/// Default value: <seealso cref="ConfigConsts.DefaultValues.MaxQueueEventCount" />
		/// </summary>
		int MaxQueueEventCount { get; }

		double MetricsIntervalInMilliseconds { get; }

		// <summary>
		// Sometimes it is necessary to sanitize the data sent to Elastic APM, e.g. remove sensitive data.
		// Configure a list of wildcard patterns of field names which should be sanitized.
		// These apply for example to HTTP headers and application/x-www-form-urlencoded data.
		// </summary>
		IReadOnlyList<WildcardMatcher> SanitizeFieldNames { get; }
		string SecretToken { get; }
		IReadOnlyList<Uri> ServerUrls { get; }
		string ServiceName { get; }
		string ServiceVersion { get; }

		/// <summary>
		/// The agent limits stack trace collection to spans with durations equal or longer than the given value
		/// 0: Disables stack trace collection for spans completely
		/// negative value: stacktrace will be collected for all spans
		/// positive value n: stacktrace will be captured for spans with a duration equal or longer than n ms.
		/// </summary>
		double SpanFramesMinDurationInMilliseconds { get; }

		/// <summary>
		/// The number of stack frames the agent collects.
		/// 0: no stacktrace is collected - This also applies to spans no matter what is the value of
		/// SpanFramesMinDurationInMilliseconds.
		/// negative: all frames must be collected
		/// positive number n: top n frames must be collected
		/// </summary>
		int StackTraceLimit { get; }

		/// <summary>
		/// 	The number of spans that are recorded per transaction.
		///  0: no spans will be collected.
		///  -1: all spans will be collected.
		/// </summary>
		int TransactionMaxSpans { get; }

		double TransactionSampleRate { get; }
	}
}
