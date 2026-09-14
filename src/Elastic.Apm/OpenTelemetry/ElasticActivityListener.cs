// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#if NET || NETSTANDARD2_1
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.OpenTelemetry
{
	public class ElasticActivityListener : IDisposable
	{
		private readonly ConditionalWeakTable<Activity, Span> _activeSpans = new();
		private readonly ConditionalWeakTable<Activity, Transaction> _activeTransactions = new();
		private readonly IApmAgent _agent;
		private readonly IApmLogger _logger;
		private volatile Tracer _tracer;
		private ActivityListener _listener;

		private volatile bool _hasServiceBusInstrumentation;
		private volatile bool _hasStorageInstrumentation;
		private volatile bool _hasCosmosDbInstrumentation;
		private volatile bool _hasMongoDbInstrumentation;
		private volatile bool _hasGrpcClientInstrumentation;
		private volatile bool _hasAspNetCoreRequestInstrumentation;

		// Initialized to the option defaults so the filter is never in a null state, which nothing matches and which
		// would therefore silently mute the bridge.
		private IReadOnlyList<WildcardMatcher> _allowedActivitySources = ConfigConsts.DefaultValues.OpenTelemetryBridgeAllowedActivitySources;
		private IReadOnlyList<WildcardMatcher> _deniedActivitySources = ConfigConsts.DefaultValues.OpenTelemetryBridgeDeniedActivitySources;

		private bool _disposed;

		/// <summary>
		/// .NET 9 introduced a set of experimental <see cref="ActivitySource"/>s covering connection level plumbing
		/// (DNS resolution, socket connect, TLS handshake and HTTP connection setup). They are dormant until a listener
		/// subscribes, so a listener is what switches them on; the bridge only subscribes to them when
		/// <see cref="IConfigurationReader.OpenTelemetryBridgeExperimentalSourcesEnabled" /> is enabled.
		/// <para>
		/// 'Experimental.System.Net.Http.Connections.ConnectionSetup' is deliberately started as the root of its own
		/// trace, because a connection is shared by many requests and outlives all of them. Promoting these activities
		/// to transactions therefore produces meaningless top level entries, such as the connection setup performed
		/// during application startup or by the agent's own transport.
		/// </para>
		/// </summary>
		private const string RuntimeInfrastructureSourcePrefix = "Experimental.System.Net.";

		/// <summary>
		/// Matches every experimental activity source, which is a wider set than
		/// <see cref="RuntimeInfrastructureSourcePrefix" /> on purpose. The 'Experimental.' prefix is the convention for
		/// telemetry whose names and attributes may still change, so whether to subscribe at all is governed by it, while
		/// the narrower prefix governs the separate question of whether connection level activities may become transactions.
		/// </summary>
		private static readonly WildcardMatcher ExperimentalActivitySources = WildcardMatcher.ValueOf("Experimental.*");

		internal ElasticActivityListener(IApmAgent agent)
		{
			_agent = agent;
			_logger = agent.Logger?.Scoped(nameof(ElasticActivityListener));
		}

		internal void Start(Tracer tracerInternal)
		{
			_tracer = tracerInternal;

			if (_listener != null)
				return;

			// Subscribe before scanning to avoid missing a load that races with the initial scan
			AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				CheckAssembly(assembly.GetName().Name);

			_logger?.Debug()?.Log(
				"ElasticActivityListener started. Detected instrumentation packages: ServiceBus={ServiceBus}, Storage={Storage}, " +
				"CosmosDb={CosmosDb}, MongoDb={MongoDb}, GrpcClient={GrpcClient}, AspNetCoreRequest={AspNetCoreRequest}",
				_hasServiceBusInstrumentation, _hasStorageInstrumentation, _hasCosmosDbInstrumentation,
				_hasMongoDbInstrumentation, _hasGrpcClientInstrumentation, _hasAspNetCoreRequestInstrumentation);

			BuildActivitySourceFilter(_agent.Configuration);
			EnableAspNetCoreRequestActivityTags();

			_listener = new ActivityListener
			{
				ActivityStarted = OnActivityStarted,
				ActivityStopped = OnActivityStopped,
				ShouldListenTo = ShouldListenTo,
				Sample = (ref _) => ActivitySamplingResult.AllData
			};

			ActivitySource.AddActivityListener(_listener);
		}

		/// <summary>
		/// The AppContext switch behind which ASP.NET Core hosting, from .NET 10, records the request method, scheme, path
		/// and server address as tags on its 'Microsoft.AspNetCore.Hosting.HttpRequestIn' activity. It defaults to
		/// suppressed, and the OpenTelemetry ASP.NET Core instrumentation reads the same switch to decide whether to add
		/// those tags itself, so enabling it does not duplicate anything in a pipeline the application configures.
		/// </summary>
		internal const string SuppressAspNetCoreActivityTagsSwitch = "Microsoft.AspNetCore.Hosting.SuppressActivityOpenTelemetryData";

		/// <summary>
		/// Without those tags a request transaction the bridge creates from the request activity can only be named after
		/// the activity, so every request shares one name. Hosting reads the switch once, when the web host starts, so
		/// this only takes effect when the agent starts first: in Program.cs before the host is built, through the
		/// profiler or startup hook, or as a hosted service under WebApplication.CreateBuilder, which starts the web
		/// server last. The documentation covers setting the switch directly for other arrangements. A value the
		/// application set explicitly is respected.
		/// </summary>
		private void EnableAspNetCoreRequestActivityTags()
		{
			if (AppContext.TryGetSwitch(SuppressAspNetCoreActivityTagsSwitch, out var suppressed))
			{
				_logger?.Debug()?.Log("AppContext switch '{Switch}' is already set to {Value}; leaving it as configured by the application.",
					SuppressAspNetCoreActivityTagsSwitch, suppressed);
				return;
			}

			AppContext.SetSwitch(SuppressAspNetCoreActivityTagsSwitch, false);

			_logger?.Debug()?.Log("Set AppContext switch '{Switch}' to false so that ASP.NET Core records request tags on its " +
				"'{ActivityName}' activity, which name a transaction the bridge creates from it.",
				SuppressAspNetCoreActivityTagsSwitch, KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn);
		}

		/// <summary>
		/// Builds the allow and deny sets used by <see cref="ShouldListenTo" />. Configuration is read once here, because
		/// <see cref="ActivityListener.ShouldListenTo" /> is evaluated once per activity source and the answer is cached by
		/// the runtime; these options are therefore deliberately not centrally configurable.
		/// </summary>
		private void BuildActivitySourceFilter(IConfigurationReader configuration)
		{
			// An IConfigurationReader implemented outside the agent may not populate these members at all. Falling back to
			// the defaults keeps a null out of the filter, which would otherwise either mute the bridge entirely or throw
			// out of the agent's constructor.
			_allowedActivitySources = configuration.OpenTelemetryBridgeAllowedActivitySources
				?? ConfigConsts.DefaultValues.OpenTelemetryBridgeAllowedActivitySources;
			var configuredDenied = configuration.OpenTelemetryBridgeDeniedActivitySources
				?? ConfigConsts.DefaultValues.OpenTelemetryBridgeDeniedActivitySources;

			// The experimental toggle is expressed as a deny entry rather than a separate rule, so there is only ever one
			// precedence to reason about: a source is observed when it matches the allow list and nothing denies it. The
			// entry exists in this effective list only; the configured denied list stays as the user wrote it.
			var experimentalEnabled = configuration.OpenTelemetryBridgeExperimentalSourcesEnabled;

			_deniedActivitySources = experimentalEnabled ? configuredDenied : [.. configuredDenied, ExperimentalActivitySources];

			// Under the profiler, environment variables are the only configuration channel and a mistake is otherwise
			// invisible, so always record what the filter actually resolved to.
			_logger?.Debug()?.Log(
				"ElasticActivityListener activity source filter: allowed=[{AllowedActivitySources}] denied=[{DeniedActivitySources}]. " +
				"Experimental activity sources are {ExperimentalActivitySourcesState}.",
				string.Join(", ", _allowedActivitySources.Select(m => m.GetMatcher())),
				string.Join(", ", _deniedActivitySources.Select(m => m.GetMatcher())),
				experimentalEnabled ? "enabled" : "disabled");
		}

		/// <summary>
		/// A source is observed when it matches the allow list and is not matched by the deny list. The allow list is a
		/// gate, the deny list is a veto, and the veto always wins. The agent's own source is always observed, whatever
		/// the filter says.
		/// </summary>
		private bool ShouldListenTo(ActivitySource source)
		{
			var observed = MatchesSourceFilter(source.Name, out var reason);
			if (!observed)
				_logger?.Trace()?.Log("Not subscribing to activity source '{SourceName}'; it {Reason}.", source.Name, reason);

			return observed;
		}

		/// <summary>
		/// The filter decision on its own, without the logging <see cref="ShouldListenTo" /> performs. Used when asking
		/// after the fact whether a source was excluded, where a 'not subscribing' log line would be misleading.
		/// </summary>
		private bool MatchesSourceFilter(string sourceName) => MatchesSourceFilter(sourceName, out _);

		private bool MatchesSourceFilter(string sourceName, out string reason)
		{
			reason = null;

			// The agent's own activity source has to have a listener at all times. Transaction.StartActivity creates the
			// activity representing a transaction through ActivitySource.CreateActivity, which returns null when nothing
			// listens to the source, and the dedicated Transaction.Listener is only registered when the bridge is disabled.
			// Without that activity, Activity.Current is unset for the duration of every transaction, so an activity started
			// inside one becomes the root of a separate trace, and an OpenTelemetry SDK the application configures itself
			// reports a different trace id than the agent. Subscribing costs nothing, because activities from this source
			// are skipped from capture through KnownListeners.SkippedActivityNamesSet.
			if (string.Equals(sourceName, Transaction.ElasticApmActivitySourceName, StringComparison.Ordinal))
				return true;

			if (!WildcardMatcher.IsAnyMatch(_allowedActivitySources, sourceName))
			{
				reason = "does not match the allowed activity sources";
				return false;
			}

			if (WildcardMatcher.IsAnyMatch(_deniedActivitySources, sourceName))
			{
				reason = "matches the denied activity sources";
				return false;
			}

			return true;
		}

		private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args) =>
			CheckAssembly(args.LoadedAssembly.GetName().Name);

		internal void CheckAssembly(string name)
		{
			if (name == "Elastic.Apm.Azure.ServiceBus")
			{
				_hasServiceBusInstrumentation = true;
				_logger?.Debug()?.Log("Detected 'Elastic.Apm.Azure.ServiceBus' — 'Microsoft.ServiceBus' activities will be skipped by the OTel bridge.");
			}
			else if (name == "Elastic.Apm.Azure.Storage")
			{
				_hasStorageInstrumentation = true;
				_logger?.Debug()?.Log("Detected 'Elastic.Apm.Azure.Storage' — 'Microsoft.Storage' activities will be skipped by the OTel bridge.");
			}
			else if (name == "Elastic.Apm.Azure.CosmosDb")
			{
				_hasCosmosDbInstrumentation = true;
				_logger?.Debug()?.Log("Detected 'Elastic.Apm.Azure.CosmosDb' — non-operation 'Microsoft.DocumentDB' activities will be skipped by the OTel bridge; " +
				"'Azure.Cosmos.Operation' activities will pass through.");
			}
			else if (name == "Elastic.Apm.MongoDb")
			{
				_hasMongoDbInstrumentation = true;
				_logger?.Debug()?.Log("Detected 'Elastic.Apm.MongoDb' — 'MongoDB.Driver' activities will be skipped by the OTel bridge.");
			}
			else if (name == "Elastic.Apm.GrpcClient")
			{
				_hasGrpcClientInstrumentation = true;
				_logger?.Debug()?.Log("Detected 'Elastic.Apm.GrpcClient' — 'Grpc.Net.Client' activities will be skipped by the OTel bridge.");
			}
			// Both of these create the transaction for an incoming ASP.NET Core request from the hosting layer's diagnostic
			// events, so the runtime's request activity would duplicate it. The Azure Functions integration does so for the
			// ASP.NET Core server the isolated worker hosts, without referencing Elastic.Apm.AspNetCore.
			else if (name == "Elastic.Apm.AspNetCore" || name == "Elastic.Apm.Azure.Functions")
			{
				_hasAspNetCoreRequestInstrumentation = true;
				_logger?.Debug()?.Log("Detected '{AssemblyName}' — '{ActivityName}' activities will be skipped by the OTel bridge " +
					"because that package creates the transaction for incoming requests.",
					name, KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn);
			}
		}

		private void OnActivityStarted(Activity activity)
		{
			if (_tracer == null)
				return;

			var config = _agent.Configuration;
			if (!config.Enabled || !config.Recording)
				return;

			if (ShouldSkipActivity(activity, out var skipReason))
			{
				LogSkippedActivity(activity, skipReason, onStart: true);
				return;
			}

			_logger?.Trace()?.Log("ActivityStarted: name:{DisplayName} id:{ActivityId} traceId:{TraceId}",
				activity.DisplayName, activity.Id, activity.TraceId);

			List<SpanLink> spanLinks = null;
			foreach (var link in activity.Links)
			{
				spanLinks ??= [];
				spanLinks.Add(new SpanLink(link.Context.SpanId.ToString(), link.Context.TraceId.ToString()));
			}

			var timestamp = TimeUtils.ToTimestamp(activity.StartTimeUtc);
			if (!CreateTransactionForActivity(activity, timestamp, spanLinks))
				CreateSpanForActivity(activity, timestamp, spanLinks);
		}

		/// <summary>
		/// Whether the activity comes from one of the runtime's connection level infrastructure sources. Matched on the
		/// source name rather than the target framework, because the net8.0 build of the agent also runs on .NET 9+.
		/// </summary>
		private static bool IsRuntimeInfrastructureActivity(Activity activity) =>
			activity.Source.Name.StartsWith(RuntimeInfrastructureSourcePrefix, StringComparison.Ordinal);

		/// <summary>
		/// Central policy for activities the OTel bridge must not capture (dedup, source-filtered fallbacks, known listeners,
		/// broken upstream sources).
		/// Used by both <see cref="OnActivityStarted"/> and <see cref="OnActivityStopped"/> so start/stop stay symmetric.
		/// </summary>
		private bool ShouldSkipActivity(Activity activity, out ActivitySkipReason skipReason)
		{
			skipReason = ActivitySkipReason.None;

			// Prevent recording of Azure Functions activities which are quite broken at the moment
			// See https://github.com/Azure/azure-functions-dotnet-worker/issues/2733
			// See https://github.com/Azure/azure-functions-dotnet-worker/issues/2875
			// See https://github.com/Azure/azure-functions-host/issues/10641
			// See https://github.com/Azure/azure-functions-dotnet-worker/issues/2810
			if ((activity.Source.Name == "" && activity.DisplayName == "InvokeFunctionAsync")
				|| activity.Source.Name == "Microsoft.Azure.Functions.Worker")
			{
				skipReason = ActivitySkipReason.AzureFunctions;
				return true;
			}

			if (KnownListeners.SkippedActivityNamesSet.Contains(activity.OperationName))
			{
				skipReason = ActivitySkipReason.KnownListener;
				return true;
			}

			// The runtime's activity for an incoming ASP.NET Core request is the transaction when nothing else provides
			// one. It is only a duplicate while an Elastic integration which creates that transaction itself is loaded;
			// without one, skipping it would leave every activity started while handling the request without a
			// transaction to nest under.
			if (_hasAspNetCoreRequestInstrumentation && IsAspNetCoreRequestActivity(activity))
			{
				skipReason = ActivitySkipReason.AspNetCoreRequestDedup;
				return true;
			}

			// Hosting can fall back to a plain Activity when its source has no listeners but request logging is enabled.
			// Apply the hosting source's filter to that fallback without excluding other sourceless activities.
			if (activity.Source.Name.Length == 0 && IsAspNetCoreRequestActivity(activity)
				&& !MatchesSourceFilter(KnownListeners.MicrosoftAspNetCoreActivitySource))
			{
				skipReason = ActivitySkipReason.AspNetCoreRequestSourceFilter;
				return true;
			}

			// If the Elastic instrumentation for an Azure service is present, skip duplicating through the OTel bridge.
			// Guard with a source name prefix check to avoid the tag lookup on non-Azure activities.
			if ((_hasServiceBusInstrumentation || _hasStorageInstrumentation || _hasCosmosDbInstrumentation)
				&& (activity.Source.Name.StartsWith("Azure.", StringComparison.Ordinal)
					|| activity.Source.Name.StartsWith("Microsoft.Azure.", StringComparison.Ordinal)))
			{
				OTelActivityMapper.TryGetStringValue(activity, SemanticConventions.AzNamespace, out var azNamespace);

				if (_hasServiceBusInstrumentation && azNamespace == "Microsoft.ServiceBus")
				{
					skipReason = ActivitySkipReason.ServiceBusDedup;
					return true;
				}

				if (_hasStorageInstrumentation && azNamespace == "Microsoft.Storage")
				{
					skipReason = ActivitySkipReason.StorageDedup;
					return true;
				}

				if (_hasCosmosDbInstrumentation && azNamespace == "Microsoft.DocumentDB"
					&& activity.Source.Name != KnownListeners.AzureCosmosOperationActivitySource)
				{
					skipReason = ActivitySkipReason.CosmosDbDedup;
					return true;
				}
			}

			if (_hasMongoDbInstrumentation && activity.Source.Name == "MongoDB.Driver")
			{
				skipReason = ActivitySkipReason.MongoDbDedup;
				return true;
			}

			if (_hasGrpcClientInstrumentation && activity.Source.Name == "Grpc.Net.Client")
			{
				skipReason = ActivitySkipReason.GrpcClientDedup;
				return true;
			}

			return false;
		}

		private void LogSkippedActivity(Activity activity, ActivitySkipReason skipReason, bool onStart)
		{
			var phase = onStart ? "ActivityStarted" : "ActivityStopped";

			switch (skipReason)
			{
				case ActivitySkipReason.AzureFunctions:
					_logger?.Trace()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} skipped Azure Functions activity " +
						"(source='{SourceName}') due to known upstream issues.", phase, activity.DisplayName, activity.Id, activity.Source.Name);
					break;
				case ActivitySkipReason.ServiceBusDedup:
					_logger?.Debug()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} traceId:{TraceId} skipped 'Microsoft.ServiceBus' " +
						"activity because 'Elastic.Apm.Azure.ServiceBus' is present in the application.",
						phase, activity.DisplayName, activity.Id, activity.TraceId);
					break;
				case ActivitySkipReason.StorageDedup:
					_logger?.Debug()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} traceId:{TraceId} skipped 'Microsoft.Storage' " +
						"activity because 'Elastic.Apm.Azure.Storage' is present in the application.",
						phase, activity.DisplayName, activity.Id, activity.TraceId);
					break;
				case ActivitySkipReason.CosmosDbDedup:
					_logger?.Debug()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} traceId:{TraceId} skipped 'Microsoft.DocumentDB' " +
						"activity because 'Elastic.Apm.Azure.CosmosDb' is present in the application.",
						phase, activity.DisplayName, activity.Id, activity.TraceId);
					break;
				case ActivitySkipReason.MongoDbDedup:
					_logger?.Debug()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} traceId:{TraceId} skipped 'MongoDB.Driver' " +
						"activity because 'Elastic.Apm.MongoDb' is present in the application.",
						phase, activity.DisplayName, activity.Id, activity.TraceId);
					break;
				case ActivitySkipReason.GrpcClientDedup:
					_logger?.Debug()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} traceId:{TraceId} skipped 'Grpc.Net.Client' " +
						"activity because 'Elastic.Apm.GrpcClient' is present in the application.",
						phase, activity.DisplayName, activity.Id, activity.TraceId);
					break;
				case ActivitySkipReason.KnownListener:
					_logger?.Trace()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} traceId:{TraceId} skipped because it matched " +
						"a skipped activity name defined in KnownListeners.", phase, activity.DisplayName, activity.Id, activity.TraceId);
					break;
				case ActivitySkipReason.AspNetCoreRequestDedup:
					_logger?.Trace()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} traceId:{TraceId} skipped ASP.NET Core request " +
						"activity because an Elastic integration which creates the request transaction is present in the application.",
						phase, activity.DisplayName, activity.Id, activity.TraceId);
					break;
				case ActivitySkipReason.AspNetCoreRequestSourceFilter:
					_logger?.Trace()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} traceId:{TraceId} skipped sourceless ASP.NET Core " +
						"request activity because '{SourceName}' is excluded by the activity source filter.",
						phase, activity.DisplayName, activity.Id, activity.TraceId, KnownListeners.MicrosoftAspNetCoreActivitySource);
					break;
			}
		}

		private enum ActivitySkipReason
		{
			None,
			AzureFunctions,
			ServiceBusDedup,
			StorageDedup,
			CosmosDbDedup,
			MongoDbDedup,
			GrpcClientDedup,
			KnownListener,
			AspNetCoreRequestDedup,
			AspNetCoreRequestSourceFilter
		}

		/// <summary>
		/// Whether the activity is the one ASP.NET Core hosting starts for each incoming request. Matched on the operation
		/// name rather than the source, because hosting falls back to a sourceless <see cref="Activity" /> when nothing
		/// listens to its 'Microsoft.AspNetCore' source.
		/// </summary>
		private static bool IsAspNetCoreRequestActivity(Activity activity) =>
			string.Equals(activity.OperationName, KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn, StringComparison.Ordinal);

		private bool CreateTransactionForActivity(Activity activity, long timestamp, List<SpanLink> spanLinks)
		{
			if (_tracer.CurrentTransaction != null)
				return false;

			// Connection level infrastructure is only meaningful within a trace that already exists. Without a current
			// transaction there is nothing to attach it to, and CreateSpanForActivity will drop it.
			if (IsRuntimeInfrastructureActivity(activity))
			{
				_logger?.Trace()?.Log("ActivityStarted: name:{DisplayName} id:{ActivityId} from runtime infrastructure source " +
					"'{SourceName}' will not be promoted to a transaction; it is only captured as a span within an existing trace.",
					activity.DisplayName, activity.Id, activity.Source.Name);

				return false;
			}

			// An in-process parent the bridge declined on purpose is never sent, so continuing the trace from its id would
			// leave the transaction pointing at a parent the server never receives. Such a parent is collapsed rather than
			// severed: the walk moves past every ancestor of that kind and continues from whatever the top-most one
			// declared as its own parent. That is a remote parent carried in from another service, whose trace context and
			// sampling decision are then honoured exactly as for a request the agent instruments itself; or a segment which
			// was captured; or nothing, in which case the activity becomes a root of its trace.
			var ancestor = activity;
			while (ancestor.Parent != null && IsDeliberatelyNotCaptured(ancestor.Parent))
				ancestor = ancestor.Parent;

			if (!ReferenceEquals(ancestor, activity))
			{
				_logger?.Trace()?.Log("ActivityStarted: name:{DisplayName} id:{ActivityId} has an in-process parent '{ParentDisplayName}' " +
					"from source '{ParentSourceName}' which the bridge deliberately did not capture; continuing the trace from " +
					"{ContinuedFrom} instead, so the transaction does not point at a parent which is never sent.",
					activity.DisplayName, activity.Id, activity.Parent.DisplayName, activity.Parent.Source.Name,
					ancestor.ParentId ?? "no parent, as a root of its trace");
			}

			// A parent id which does not parse, such as one in the hierarchical format, falls back to a root as well.
			var dt = ancestor.ParentId != null
				? TraceContext.TryExtractTracingData(ancestor.ParentId, ancestor.TraceStateString)
				: null;

			var transaction = _tracer.StartTransactionInternal(activity.DisplayName, "unknown",
				timestamp, true, activity.SpanId.ToString(), activity.TraceId.ToString(),
				distributedTracingData: dt, links: spanLinks?.Count > 0 ? spanLinks : null, current: activity);

			transaction.Otel = new OTel { SpanKind = activity.Kind.ToString() };

			// An incoming request records its method and URL when the activity is created, which is early enough for an
			// error captured while the request is handled to copy them: the error takes the transaction's context as it
			// stands at that moment. Tags added later are picked up when the activity stops.
			OTelActivityMapper.TryUpdateHttpRequestContext(transaction, activity);

			_activeTransactions.AddOrUpdate(activity, transaction);

			_logger?.Trace()?.Log("Created transaction id:{TransactionId} name:{Name} for activity id:{ActivityId}",
				transaction.Id, transaction.Name, activity.Id);

			return true;
		}

		/// <summary>
		/// Whether the bridge saw this activity and declined it on purpose: <see cref="ShouldSkipActivity" /> rejects it,
		/// such as 'System.Net.Http.HttpRequestOut', or its source is excluded by the activity source filter. Such an
		/// activity is never sent.
		/// <para>
		/// Absence from the tables on its own does not establish this. A parent the bridge never saw, one created before
		/// the agent started or belonging to a source only another listener observes, is absent too, and an OpenTelemetry
		/// pipeline the application configures itself may well have exported it under the very id the child declares.
		/// Where we cannot tell, the declared parent is kept rather than rewriting the topology. The agent's own
		/// transaction activity is skipped from capture as well, but its span id is the id of a transaction which is
		/// sent, so it counts as captured here.
		/// </para>
		/// </summary>
		private bool IsDeliberatelyNotCaptured(Activity parent) =>
			!_activeTransactions.TryGetValue(parent, out _)
			&& !_activeSpans.TryGetValue(parent, out _)
			&& !string.Equals(parent.OperationName, KnownListeners.ApmTransactionActivityName, StringComparison.Ordinal)
			&& (ShouldSkipActivity(parent, out _) || !MatchesSourceFilter(parent.Source.Name));

		private void CreateSpanForActivity(Activity activity, long timestamp, List<SpanLink> spanLinks)
		{
			// The activity declares a parent we did not create in this process (Activity.Parent is null while a ParentSpanId
			// is set), which is what a messaging consumer does when it continues the producer's context. We are about to nest
			// it under the ambient segment instead, so record the declared parent as a link rather than discarding it.
			// A null Parent does not prove the declared parent is remote: an activity started from an ActivityContext the
			// caller already had in hand arrives the same way. When that context belongs to the trace we are nesting into,
			// it is an ancestor or sibling already present in this trace, whichever segment we happen to nest under, and a
			// link would only point back into the same waterfall. The same goes for a context the caller linked explicitly.
			if (activity.Parent is null && activity.ParentSpanId != default)
			{
				var declaredParentId = activity.ParentSpanId.ToString();
				var declaredTraceId = activity.TraceId.ToString();

				var isInCurrentTrace = string.Equals(declaredTraceId, _tracer.CurrentTransaction?.TraceId, StringComparison.Ordinal);
				var isAlreadyLinked = ContainsLink(spanLinks, declaredParentId, declaredTraceId);

				if (isInCurrentTrace || isAlreadyLinked)
				{
					_logger?.Trace()?.Log("ActivityStarted: name:{DisplayName} id:{ActivityId} declares parent {ParentSpanId} " +
						"which is {SpanLinkSkipReason}; no span link added.", activity.DisplayName, activity.Id, activity.ParentSpanId,
						isInCurrentTrace ? "part of the trace this span is nested into" : "already present in the activity's links");
				}
				else
				{
					spanLinks ??= [];
					spanLinks.Add(new SpanLink(declaredParentId, declaredTraceId));

					_logger?.Trace()?.Log("ActivityStarted: name:{DisplayName} id:{ActivityId} declares remote parent {ParentSpanId} " +
						"in trace {ParentTraceId}; captured as a span link because the activity is nested under the current span.",
						activity.DisplayName, activity.Id, activity.ParentSpanId, activity.TraceId);
				}
			}

			Span newSpan;
			if (_tracer.CurrentSpan == null)
			{
				newSpan = (_tracer.CurrentTransaction as Transaction)?.StartSpanInternal(activity.DisplayName, "unknown",
					timestamp: timestamp, id: activity.SpanId.ToString(), links: spanLinks?.Count > 0 ? spanLinks : null, current: activity);
			}
			else
			{
				newSpan = (_tracer.CurrentSpan as Span)?.StartSpanInternal(activity.DisplayName, "unknown",
					timestamp: timestamp, id: activity.SpanId.ToString(), links: spanLinks?.Count > 0 ? spanLinks : null, current: activity);
			}

			if (newSpan == null)
			{
				_logger?.Trace()?.Log("ActivityStarted: name:{DisplayName} id:{ActivityId} — could not create span (parent transaction/span is absent or non-recording). Activity will not be captured.",
					activity.DisplayName, activity.Id);
				return;
			}

			newSpan.Otel = new OTel { SpanKind = activity.Kind.ToString() };

			if (activity.Kind == ActivityKind.Internal)
			{
				newSpan.Type = "app";
				newSpan.Subtype = "internal";
			}

			_activeSpans.AddOrUpdate(activity, newSpan);

			_logger?.Trace()?.Log("Created span id:{SpanId} name:{Name} for activity id:{ActivityId}",
				newSpan.Id, newSpan.Name, activity.Id);
		}

		private static bool ContainsLink(List<SpanLink> links, string spanId, string traceId)
		{
			if (links == null)
				return false;

			foreach (var link in links)
			{
				if (link.SpanId == spanId && link.TraceId == traceId)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Some activities, including the runtime's own System.Net sources, only set their human readable
		/// <see cref="Activity.DisplayName"/> when they stop, and instrumentation libraries change it once more is
		/// known, such as the route of an incoming request. The final name is the one any OpenTelemetry exporter would
		/// report, so adopt it, unless the name was set through the Elastic API in the meantime, which the segment
		/// records itself and which always wins.
		/// </summary>
		private static bool TryGetLateDisplayName(Activity activity, string currentName, bool hasCustomName, out string displayName)
		{
			displayName = activity.DisplayName;
			return !hasCustomName && !string.Equals(displayName, currentName, StringComparison.Ordinal);
		}

		private void OnActivityStopped(Activity activity)
		{
			if (activity == null)
			{
				_logger?.Trace()?.Log("ActivityStopped called with `null` activity. Ignoring `null` activity.");
				return;
			}

			if (_tracer == null)
				return;

			var config = _agent.Configuration;
			if (!config.Enabled || !config.Recording)
				return;

			if (ShouldSkipActivity(activity, out _))
				return;

			_logger?.Trace()?.Log("ActivityStopped: name:{DisplayName} id:{ActivityId} traceId:{TraceId}",
				activity.DisplayName, activity.Id, activity.TraceId);

			if (_activeTransactions.TryGetValue(activity, out var transaction))
			{
				_activeTransactions.Remove(activity);
				transaction.Duration = activity.Duration.TotalMilliseconds;

				if (TryGetLateDisplayName(activity, transaction.Name, transaction.HasCustomName, out var displayName))
				{
					transaction.Name = displayName;
				}
				else if (!transaction.HasCustomName && IsAspNetCoreRequestActivity(activity)
					&& string.Equals(activity.DisplayName, activity.OperationName, StringComparison.Ordinal)
					&& OTelActivityMapper.TryGetHttpServerRequestName(activity, out var requestName))
				{
					// ASP.NET Core hosting never sets a display name of its own, so without an instrumentation library the
					// transaction would carry the operation name and every request would share it. From .NET 10 the runtime
					// records the request method and path as tags, see EnableAspNetCoreRequestActivityTags, which give a name
					// of the same shape the ASP.NET Core integration produces. Only replace the default operation name,
					// preserving display names supplied by instrumentation even before the activity started.
					transaction.Name = requestName;
				}

				OTelActivityMapper.UpdateOTelAttributes(activity, transaction.Otel);
				OTelActivityMapper.InferTransactionType(transaction, activity);

				// For the tags an instrumentation library adds after the activity was created; a no-op once filled.
				OTelActivityMapper.TryUpdateHttpRequestContext(transaction, activity);

				transaction.Outcome = Outcome.Unknown;
#if NET // Not available in netstandard2.1
				transaction.Outcome = ActivityStatusToOutcome(activity.Status);
#endif
				transaction.End();
			}
			else if (_activeSpans.TryGetValue(activity, out var span))
			{
				_activeSpans.Remove(activity);
				UpdateSpan(activity, span);
			}
			else
			{
				_logger?.Trace()?.Log("ActivityStopped: name:{DisplayName} id:{ActivityId} — activity was not tracked (no matching span or transaction).",
					activity.DisplayName, activity.Id);
			}
		}

		private static void UpdateSpan(Activity activity, Span span)
		{
			span.Duration = activity.Duration.TotalMilliseconds;

			if (TryGetLateDisplayName(activity, span.Name, span.HasCustomName, out var displayName))
				span.Name = displayName;

			OTelActivityMapper.UpdateOTelAttributes(activity, span.Otel);
			OTelActivityMapper.InferSpanTypeAndSubType(span, activity);

			span.Outcome = Outcome.Unknown;
#if NET // Not available in netstandard2.1
			span.Outcome = ActivityStatusToOutcome(activity.Status);
#endif
			span.End();
		}

		/// <summary>
		/// Specifically exposed for benchmarking. This is not intended for any other purpose.
		/// </summary>
		internal static void UpdateSpanBenchmark(Activity activity, Span span) => UpdateSpan(activity, span);

#if NET
		private static Outcome ActivityStatusToOutcome(ActivityStatusCode status) => status switch
		{
			ActivityStatusCode.Ok    => Outcome.Success,
			ActivityStatusCode.Error => Outcome.Failure,
			_                        => Outcome.Unknown
		};
#endif

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_logger?.Debug()?.Log("ElasticActivityListener disposing.");
					AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
					_listener?.Dispose();
					_listener = null;
				}

				_disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
#endif
