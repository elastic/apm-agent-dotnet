// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	public interface IConfigurationReader
	{
		string ApiKey { get; }

		/// <summary>
		/// When defined, all namespaces not starting with one of the values of this collection are ignored when determining
		/// Exception culprit.
		/// This suppresses any configuration of <see cref="ExcludedNamespaces" />
		/// </summary>
		IReadOnlyCollection<string> ApplicationNamespaces { get; }

		string CaptureBody { get; }
		List<string> CaptureBodyContentTypes { get; }
		bool CaptureHeaders { get; }
		bool CentralConfig { get; }

		/// <summary>
		/// Specify which cloud provider should be assumed for metadata collection. By default, the agent will attempt to detect the cloud
		/// provider or, if that fails, will use trial and error to collect the metadata. Valid options are "aws", "gcp", and "azure".
		/// If this config value is set to "False", no cloud metadata will be collected.
		/// </summary>
		string CloudProvider { get; }

		/// <summary>
		/// Disables the collection of certain metrics. If the name of a metric matches any of the wildcard expressions, it will
		/// not be collected
		/// </summary>
		IReadOnlyList<WildcardMatcher> DisableMetrics { get; }

		/// <summary>
		/// Enables the agent.
		/// When set to <c>true</c> (the default), the agent is enabled.
		/// When set to <c>false</c>, the agent is disabled, including instrumentation and remote config polling.
		/// The value of <see cref="Enabled" /> cannot be changed during the lifetime of the application.
		/// <para />
		/// To dynamically change agent operation, use <see cref="Recording" />
		/// </summary>
		bool Enabled { get; }

		string Environment { get; }

		/// <summary>
		/// A list of namespaces to exclude when reading an exception's StackTrace to determine the culprit.
		/// Namespaces are checked with string.StartsWith() so "System." matches all System namespaces
		/// </summary>
		IReadOnlyCollection<string> ExcludedNamespaces { get; }

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

		IReadOnlyDictionary<string, string> GlobalLabels { get; }

		/// <summary>
		/// Allows for the reported hostname to be manually specified. If unset, the hostname will be detected.
		/// </summary>
		string HostName { get; }

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

		/// <summary>
		/// Whether the agent is recording.
		/// When set to <c>true</c>. the agent instruments and capture requests, tracks errors, and
		/// collects and sends metrics.
		/// When set to <c>false</c>, the agent does not collect data or communicate with the APM server, except to
		/// fetch central configuration.
		/// Recording can be changed during the lifetime of the application.
		/// </summary>
		/// <remarks>
		/// As this is a reversible switch, agent threads are not terminated when inactivated, but they will be mostly
		/// idle in this state, so the overhead should be negligible.
		/// </remarks>
		public bool Recording { get; }

		/// <summary>
		/// Sometimes it is necessary to sanitize the data sent to Elastic APM, e.g. remove sensitive data.
		/// Configure a list of wildcard patterns of field names which should be sanitized.
		/// These apply for example to HTTP headers and application/x-www-form-urlencoded data.
		/// </summary>
		IReadOnlyList<WildcardMatcher> SanitizeFieldNames { get; }
		string SecretToken { get; }

		/// <summary>
		/// The URLs for APM server.
		/// </summary>
		[Obsolete("Use ServerUrl")]
		IReadOnlyList<Uri> ServerUrls { get; }

		/// <summary>
		/// The URL for APM server
		/// </summary>
		Uri ServerUrl { get; }

		string ServiceName { get; }

		string ServiceNodeName { get; }
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
		/// A list of patterns to match HTTP requests to ignore. An incoming HTTP request whose request line matches any of the
		/// patterns will not be reported as a transaction.
		/// </summary>
		IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls { get; }

		/// <summary>
		/// 	The number of spans that are recorded per transaction.
		///  0: no spans will be collected.
		///  -1: all spans will be collected.
		/// </summary>
		int TransactionMaxSpans { get; }

		double TransactionSampleRate { get; }

		/// <summary>
		/// If true, for all outgoing HTTP requests the agent stores the traceparent in a header prefixed with elastic-apm
		/// (elastic-apm-traceparent)
		/// otherwise it'll use the official header name from w3c, which is "traceparewnt".
		/// </summary>
		bool UseElasticTraceparentHeader { get; }

		/// <summary>
		/// The agent verifies the server's certificate if an HTTPS connection to the APM server is used.
		/// Verification can be disabled by setting to <c>false</c>.
		/// </summary>
		bool VerifyServerCert { get; }
	}
}
