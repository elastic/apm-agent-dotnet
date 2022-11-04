// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm.CentralConfig;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Report;
using Elastic.Apm.ServerInfo;
#if NET5_0 || NET6_0
using Elastic.Apm.OpenTelemetry;
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
				var tempLogger = logger ?? ConsoleLogger.LoggerOrDefault(configurationReader?.LogLevel);
				ConfigurationReader = configurationReader ?? new EnvironmentConfigurationReader(tempLogger);
				Logger = logger ?? ConsoleLogger.LoggerOrDefault(ConfigurationReader.LogLevel);
				PrintAgentLogPreamble(Logger);
				Service = Service.GetDefaultService(ConfigurationReader, Logger);

				var systemInfoHelper = new SystemInfoHelper(Logger);
				var system = systemInfoHelper.GetSystemInfo(ConfigurationReader.HostName);

				ConfigurationStore = new ConfigurationStore(new ConfigurationSnapshotFromReader(ConfigurationReader, "local"), Logger);

				ApmServerInfo = apmServerInfo ?? new ApmServerInfo();

				// Called by PayloadSenderV2 after the ServerInfo is fetched
				Action<bool, IApmServerInfo> serverInfoCallback = null;

#if NET5_0 || NET6_0
				ElasticActivityListener activityListener = null;
				if (ConfigurationReader.EnableOpenTelemetryBridge)
				{
					activityListener = new ElasticActivityListener(this);

					serverInfoCallback = (success, serverInfo) =>
					{
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
								activityListener?.Dispose();
							}
						}
						else
						{
							Logger.Warning()
								?.Log(
									"Unable to read server version - OpenTelemetry (Activity) bridge is only supported with APM Server 7.16.0 or newer. "
									+ "The bridge remains active, but due to unknown server version it may not work as expected.");
						}
					};
				}
#endif
				PayloadSender = payloadSender
					?? new PayloadSenderV2(Logger, ConfigurationStore.CurrentSnapshot, Service, system, ApmServerInfo,
						isEnabled: ConfigurationReader.Enabled, serverInfoCallback:serverInfoCallback);

				if (ConfigurationReader.Enabled)
					breakdownMetricsProvider ??= new BreakdownMetricsProvider(Logger);

				HttpTraceConfiguration = new HttpTraceConfiguration();
				SubscribedListeners = new HashSet<Type>();

				// initialize the tracer before central configuration or metric collection is started
				TracerInternal = new Tracer(Logger, Service, PayloadSender, ConfigurationStore,
					currentExecutionSegmentsContainer ?? new CurrentExecutionSegmentsContainer(), ApmServerInfo, breakdownMetricsProvider);

#if NET5_0 || NET6_0
				if (ConfigurationReader.EnableOpenTelemetryBridge)
				{
					// If the server version is not known yet, we enable the listener - and then the callback will do the version check once we have the version
					if (ApmServerInfo.Version == null || ApmServerInfo?.Version == new ElasticVersion(0, 0, 0, null))
						activityListener?.Start(TracerInternal);
					// Otherwise do a version check
					else if (ApmServerInfo.Version >= new ElasticVersion(7, 16, 0, string.Empty))
					{
						Logger.Info()
							?.Log("Starting OpenTelemetry (Activity) bridge");

						activityListener?.Start(TracerInternal);
					}
					else
					{
						Logger.Warning()
							?.Log(
								"OpenTelemetry (Activity) bridge is only supported with APM Server 7.16.0 or newer - bridge won't be enabled. Current Server version: {version}",
								ApmServerInfo.Version.ToString());
						activityListener?.Dispose();
					}
				}
#endif

				if (ConfigurationReader.Enabled)
				{
					CentralConfigurationFetcher = centralConfigurationFetcher ?? new CentralConfigurationFetcher(Logger, ConfigurationStore, Service);
					MetricsCollector = metricsCollector ?? new MetricsCollector(Logger, PayloadSender, ConfigurationStore, breakdownMetricsProvider);
					MetricsCollector.StartCollecting();
				}
				else
					Logger.Info()?.Log("The Elastic APM .NET Agent is disabled - the agent won't capture traces and metrics.");
			}
			catch (Exception e)
			{
				Logger.Error()?.LogException(e, "Failed initializing agent.");
			}
		}

		internal ICentralConfigurationFetcher CentralConfigurationFetcher { get; }

		internal IConfigurationStore ConfigurationStore { get; }

		public IConfigurationReader ConfigurationReader { get; }

		public IApmLogger Logger { get; }

		private IMetricsCollector MetricsCollector { get; }

		public IPayloadSender PayloadSender { get; }

		internal IApmServerInfo ApmServerInfo { get; }

		internal HttpTraceConfiguration HttpTraceConfiguration { get; }

		internal HashSet<Type> SubscribedListeners { get; }

		/// <summary>
		/// Identifies the monitored service. If this remains unset the agent
		/// automatically populates it based on the entry assembly.
		/// </summary>
		/// <value>The service.</value>
		public Service Service { get; }

		public ITracer Tracer => TracerInternal;

		internal Tracer TracerInternal { get; }

		public void Dispose()
		{
			if (MetricsCollector is IDisposable disposableMetricsCollector) disposableMetricsCollector.Dispose();

			if (PayloadSender is IDisposable disposablePayloadSender) disposablePayloadSender.Dispose();

			CentralConfigurationFetcher?.Dispose();
		}

		private static void PrintAgentLogPreamble(IApmLogger logger)
		{
			if (logger?.Info() != null)
			{
				try
				{
					var info = logger.Info().Value;
					info.Log("********************************************************************************");
					info.Log(
						$"Elastic APM .NET Agent, version: {Assembly.GetExecutingAssembly().GetName().Version}, file creation time: {File.GetCreationTime(Assembly.GetExecutingAssembly().Location).ToUniversalTime()} UTC");
					info.Log($"Process ID: {Process.GetCurrentProcess().Id}");
					info.Log($"Process Name: {Process.GetCurrentProcess().ProcessName}");
					info.Log($"Operating System: {RuntimeInformation.OSDescription}");
					info.Log($"CPU architecture: {RuntimeInformation.OSArchitecture}");
					info.Log($"Host: {Environment.MachineName}");
					info.Log($"Runtime: {RuntimeInformation.FrameworkDescription}");
					info.Log($"Time zone: {TimeZoneInfo.Local}");
					info.Log("********************************************************************************");
				}
				catch (Exception e)
				{
					logger?.Warning()?.LogException(e, $"Unexpected exception during {nameof(PrintAgentLogPreamble)}");
				}
			}
		}
	}
}
