// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm.CentralConfig;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Features;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Report;
using Elastic.Apm.ServerInfo;
#if NET5_0_OR_GREATER
using Elastic.Apm.OpenTelemetry;
#endif

#if NETFRAMEWORK
using Elastic.Apm.Config.Net4FullFramework;
#endif

namespace Elastic.Apm
{
	public class AgentComponents : IApmAgent, IDisposable
	{
		public AgentComponents(
			IApmLogger logger = null,
			IConfigurationReader configurationReader = null,
			IPayloadSender payloadSender = null
		) : this(logger, configurationReader, payloadSender, null, null, null, null) { }

		internal AgentComponents(
			IApmLogger logger,
			IConfigurationReader configurationReader,
			IPayloadSender payloadSender,
			IMetricsCollector metricsCollector,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer,
			ICentralConfigurationFetcher centralConfigurationFetcher,
			IApmServerInfo apmServerInfo,
			BreakdownMetricsProvider breakdownMetricsProvider = null
		)
		{
			try
			{
				ConfigurationReader = CreateConfiguration(logger, configurationReader);
				Logger = logger ?? CheckForProfilerLogger(DefaultLogger(null, configurationReader), ConfigurationReader.LogLevel);
				Service = Service.GetDefaultService(ConfigurationReader, Logger);

				var systemInfoHelper = new SystemInfoHelper(Logger);
				var system = systemInfoHelper.GetSystemInfo(ConfigurationReader.HostName);

				ConfigurationStore = new ConfigurationStore(new RuntimeConfigurationSnapshot(ConfigurationReader), Logger);

				ApmServerInfo = apmServerInfo ?? new ApmServerInfo();
				HttpTraceConfiguration = new HttpTraceConfiguration();

#if NET5_0_OR_GREATER
				// Initialize early because ServerInfoCallback requires it and might execute
				// before EnsureElasticActivityStarted runs
				ElasticActivityListener = new ElasticActivityListener(this, HttpTraceConfiguration);
#endif
				PayloadSender = payloadSender
					?? new PayloadSenderV2(Logger, ConfigurationStore.CurrentSnapshot, Service, system,
						ApmServerInfo,
						isEnabled: ConfigurationReader.Enabled, serverInfoCallback: ServerInfoCallback);

				if (ConfigurationReader.Enabled)
					breakdownMetricsProvider ??= new BreakdownMetricsProvider(Logger);

				SubscribedListeners = new HashSet<Type>();

				// initialize the tracer before central configuration or metric collection is started
				TracerInternal = new Tracer(Logger, Service, PayloadSender, ConfigurationStore,
					currentExecutionSegmentsContainer ?? new CurrentExecutionSegmentsContainer(), ApmServerInfo,
					breakdownMetricsProvider);

#if NET5_0_OR_GREATER
				EnsureElasticActivityStarted();
#endif

				if (ConfigurationReader.Enabled)
				{
					var agentFeatures = AgentFeaturesProvider.Get(Logger);
					//
					// Central configuration
					//
					if (centralConfigurationFetcher != null)
						CentralConfigurationFetcher = centralConfigurationFetcher;
					else if (agentFeatures.Check(AgentFeature.RemoteConfiguration))
						CentralConfigurationFetcher = new CentralConfigurationFetcher(Logger, ConfigurationStore, Service);
					//
					// Metrics collection
					//
					if (metricsCollector != null)
						MetricsCollector = metricsCollector;
					else if (agentFeatures.Check(AgentFeature.MetricsCollection))
						MetricsCollector = new MetricsCollector(Logger, PayloadSender, ConfigurationStore, breakdownMetricsProvider);
					MetricsCollector?.StartCollecting();
				}
				else
				{
					Logger.Info()
						?.Log("The Elastic APM .NET Agent is disabled - the agent won't capture traces and metrics.");
				}
			}
			catch (Exception e)
			{
				Logger.Error()?.LogException(e, "Failed initializing agent.");
			}
		}

		private void EnsureElasticActivityStarted()
		{
#if !NET5_0_OR_GREATER
			return;
#else
			if (!ConfigurationReader.OpenTelemetryBridgeEnabled) return;

			// If the server version is not known yet, we enable the listener - and then the callback will do the version check once we have the version
			if (ApmServerInfo.Version == null || ApmServerInfo.Version == new ElasticVersion(0, 0, 0, null))
				ElasticActivityListener?.Start(TracerInternal);
			// Otherwise do a version check
			else if (ApmServerInfo.Version >= new ElasticVersion(7, 16, 0, string.Empty))
			{
				Logger.Info()?.Log("Starting OpenTelemetry (Activity) bridge");
				ElasticActivityListener?.Start(TracerInternal);
			}
			else
			{
				Logger.Warning()
					?.Log(
						"OpenTelemetry (Activity) bridge is only supported with APM Server 7.16.0 or newer - bridge won't be enabled. Current Server version: {version}",
						ApmServerInfo.Version.ToString());
				ElasticActivityListener?.Dispose();
			}
#endif
		}

		private void ServerInfoCallback(bool success, IApmServerInfo serverInfo)
		{
#if !NET5_0_OR_GREATER
			return;
#else
			if (success)
			{
				if (serverInfo.Version >= new ElasticVersion(7, 16, 0, string.Empty))
				{
					Logger.Info()
						?.Log("APM Server version ready - OpenTelemetry (Activity) bridge is active. Current Server version: {version}",
							serverInfo.Version.ToString());
				}
				else
				{
					Logger.Warning()
						?.Log(
							"OpenTelemetry (Activity) bridge is only supported with APM Server 7.16.0 or newer - bridge won't be enabled. Current Server version: {version}",
							serverInfo.Version.ToString());
					ElasticActivityListener?.Dispose();
				}
			}
			else
			{
				Logger.Warning()
					?.Log(
						"Unable to read server version - OpenTelemetry (Activity) bridge is only supported with APM Server 7.16.0 or newer. "
						+ "The bridge remains active, but due to unknown server version it may not work as expected.");
			}
#endif
		}

		private static IApmLogger DefaultLogger(IApmLogger logger, IConfigurationReader configurationReader)
		{
#if NETFRAMEWORK
			return logger ?? FullFrameworkDefaultImplementations.CreateDefaultLogger();
#else
			return logger ?? ConsoleLogger.LoggerOrDefault(configurationReader?.LogLevel);
#endif
		}

		private static IConfigurationReader CreateConfiguration(IApmLogger logger, IConfigurationReader configurationReader)
		{
			var configurationLogger = DefaultLogger(logger, configurationReader);

#if NETFRAMEWORK
			return configurationReader
					?? FullFrameworkDefaultImplementations.CreateConfigurationReaderFromConfiguredType(configurationLogger)
					?? new AppSettingsConfiguration(configurationLogger);
#else
			return configurationReader ?? new EnvironmentConfiguration(configurationLogger);
#endif
		}


		//
		// This is the hooking point that checks for the existence of profiler-related
		// logging settings.
		// If no agent logging is configured but we detect profiler logging settings, those
		// will be honoured.
		// The finer-grained log-level (agent vs profiler) will be used.
		// This has the benefit that users will also get agent logs in addition to profiler-only
		// logs.
		//
		internal static IApmLogger CheckForProfilerLogger(IApmLogger fallbackLogger, LogLevel agentLogLevel, IDictionary environmentVariables = null)
		{
			try
			{
				var profilerLogConfig = ProfilerLogConfig.Check(environmentVariables);
				if (profilerLogConfig.IsActive)
				{
					var effectiveLogLevel = LogLevelUtils.GetFinest(agentLogLevel, profilerLogConfig.LogLevel);

					if ((profilerLogConfig.LogTargets & ProfilerLogTarget.File) == ProfilerLogTarget.File)
						TraceLogger.TraceSource.Listeners.Add(new TextWriterTraceListener(profilerLogConfig.LogFilePath));
					if ((profilerLogConfig.LogTargets & ProfilerLogTarget.StdOut) == ProfilerLogTarget.StdOut)
						TraceLogger.TraceSource.Listeners.Add(new TextWriterTraceListener(Console.Out));

					var logger = new TraceLogger(effectiveLogLevel);
					logger.Info()?.Log($"{nameof(ProfilerLogConfig)} - {profilerLogConfig}");
					return logger;
				}
			}
			catch (Exception e)
			{
				fallbackLogger.Warning()?.LogException(e, "Error in CheckForProfilerLogger");
			}
			return fallbackLogger;
		}

#if NET5_0_OR_GREATER
		private ElasticActivityListener ElasticActivityListener { get; }
#endif

		internal ICentralConfigurationFetcher CentralConfigurationFetcher { get; }

		internal IConfigurationStore ConfigurationStore { get; }

		internal IMetricsCollector MetricsCollector { get; }

		internal IApmServerInfo ApmServerInfo { get; }

		internal HttpTraceConfiguration HttpTraceConfiguration { get; }

		internal HashSet<Type> SubscribedListeners { get; }

		internal Tracer TracerInternal { get; }

		public IConfigurationReader ConfigurationReader { get; }

		public IApmLogger Logger { get; }

		public IPayloadSender PayloadSender { get; }

		/// <summary>
		/// Identifies the monitored service. If this remains unset the agent
		/// automatically populates it based on the entry assembly.
		/// </summary>
		/// <value>The service.</value>
		public Service Service { get; }

		public ITracer Tracer => TracerInternal;

		public void Dispose()
		{
			if (MetricsCollector is IDisposable disposableMetricsCollector)
				disposableMetricsCollector.Dispose();

			if (PayloadSender is IDisposable disposablePayloadSender)
				disposablePayloadSender.Dispose();
			CentralConfigurationFetcher?.Dispose();
#if NET5_0_OR_GREATER
			ElasticActivityListener?.Dispose();
#endif
		}
	}
}
