using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	public abstract class AbstractConfigurationReader
	{
		protected AbstractConfigurationReader(IApmLogger logger) => ScopedLogger = logger?.Scoped(GetType().Name);

		protected IApmLogger Logger => ScopedLogger;

		private ScopedLogger ScopedLogger { get; }

		protected static ConfigurationKeyValue Kv(string key, string value, string origin) =>
			new ConfigurationKeyValue(key, value, origin);

		protected internal static bool TryParseLogLevel(string value, out LogLevel level)
		{
			level = default;
			if (string.IsNullOrEmpty(value)) return false;

			var retLevel = DefaultLogLevel();
			if (!retLevel.HasValue) return false;

			level = retLevel.Value;
			return true;

			LogLevel? DefaultLogLevel()
			{
				switch (value.ToLowerInvariant())
				{
					case "trace": return LogLevel.Trace;
					case "debug": return LogLevel.Debug;
					case "information":
					case "info": return LogLevel.Information;
					case "warning": return LogLevel.Warning;
					case "error": return LogLevel.Error;
					case "critical": return LogLevel.Critical;
					case "none": return LogLevel.None;
					default: return null;
				}
			}
		}

		protected string ParseSecretToken(ConfigurationKeyValue kv)
		{
			if (kv == null || string.IsNullOrEmpty(kv.Value)) return null;

			return kv.Value;
		}

		protected bool ParseCaptureHeaders(ConfigurationKeyValue kv)
		{
			if (kv == null || string.IsNullOrEmpty(kv.Value)) return true;

			return !bool.TryParse(kv.Value, out var value) || value;
		}

		protected LogLevel ParseLogLevel(ConfigurationKeyValue kv)
		{
			if (TryParseLogLevel(kv?.Value, out var level)) return level;

			if (kv?.Value == null)
				Logger?.Debug()?.Log("No log level provided. Defaulting to log level '{DefaultLogLevel}'", ConsoleLogger.DefaultLogLevel);
			else
			{
				Logger?.Error()
					?.Log("Failed parsing log level from {Origin}: {Key}, value: {Value}. Defaulting to log level '{DefaultLogLevel}'",
						kv.ReadFrom, kv.Key, kv.Value, ConsoleLogger.DefaultLogLevel);
			}

			return ConsoleLogger.DefaultLogLevel;
		}

		protected IReadOnlyList<Uri> ParseServerUrls(ConfigurationKeyValue kv)
		{
			var list = new List<Uri>();
			if (kv == null || string.IsNullOrEmpty(kv.Value)) return LogAndReturnDefault().AsReadOnly();

			var uriStrings = kv.Value.Split(',');
			foreach (var u in uriStrings)
			{
				if (TryParseUri(u, out var uri))
				{
					list.Add(uri);
					continue;
				}

				Logger?.Error()?.Log("Failed parsing server URL from {Origin}: {Key}, value: {Value}", kv.ReadFrom, kv.Key, u);
			}

			return list.Count == 0 ? LogAndReturnDefault().AsReadOnly() : list.AsReadOnly();

			List<Uri> LogAndReturnDefault()
			{
				list.Add(ConfigConsts.DefaultValues.ServerUri);
				Logger?.Debug()?.Log("Using default ServerUrl: {ServerUrl}", ConfigConsts.DefaultValues.ServerUri);
				return list;
			}

			bool TryParseUri(string u, out Uri uri)
			{
				// https://stackoverflow.com/a/33573337
				uri = null;
				if (!Uri.IsWellFormedUriString(u, UriKind.Absolute)) return false;
				if (!Uri.TryCreate(u, UriKind.Absolute, out uri)) return false;

				return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
			}
		}

		protected double ParseMetricsInterval(ConfigurationKeyValue kv)
		{
			string value;
			if (kv == null || string.IsNullOrWhiteSpace(kv.Value))
				value = ConfigConsts.DefaultValues.MetricsInterval;
			else
				value = kv.Value;

			double valueInMilliseconds;

			try
			{
				if (!TryParseTimeInterval(value, out valueInMilliseconds, TimeSuffix.S))
				{
					Logger?.Error()
						?.Log("Failed to parse provided metrics interval `{ProvidedMetricsInterval}' - " +
							"using default: {DefaultMetricsInterval}",
							value,
							ConfigConsts.DefaultValues.MetricsInterval);
					return ConfigConsts.DefaultValues.MetricsIntervalInMilliseconds;
				}
			}
			catch (ArgumentException e)
			{
				Logger?.Error()
					?.LogException(e, "Failed to parse provided metrics interval `{ProvidedMetricsInterval}' - " +
						"using default: {DefaultMetricsInterval}",
						value,
						ConfigConsts.DefaultValues.MetricsInterval);

				return ConfigConsts.DefaultValues.MetricsIntervalInMilliseconds;
			}

			// ReSharper disable once CompareOfFloatsByEqualityOperator - we compare to exactly zero here
			if (valueInMilliseconds == 0)
				return valueInMilliseconds;

			if (valueInMilliseconds < 0)
			{
				Logger?.Error()
					?.Log("Provided metrics interval `{ProvidedMetricsInterval}' is negative - " +
						"metrics collection will be disabled",
						value);
				return 0;
			}

			if (valueInMilliseconds < ConfigConsts.Constraints.MinMetricsIntervalInMilliseconds)
			{
				Logger?.Error()
					?.Log("Provided metrics interval `{ProvidedMetricsInterval}' is smaller than allowed minimum: {MinProvidedMetricsInterval}ms - " +
						"metrics collection will be disabled",
						value,
						ConfigConsts.Constraints.MinMetricsIntervalInMilliseconds);
				return 0;
			}

			return valueInMilliseconds;
		}

		protected int ParseStackTraceLimit(ConfigurationKeyValue kv)
		{
			if (kv == null || string.IsNullOrWhiteSpace(kv.Value))
				return ConfigConsts.DefaultValues.StackTraceLimit;


			if (int.TryParse(kv.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
				return result;

			Logger?.Error()
				?.Log("Failed to parse provided stack trace limit `{ProvidedStackTraceLimit}` - using default: {DefaultStackTraceLimit}",
					kv.Value, ConfigConsts.DefaultValues.StackTraceLimit);

			return ConfigConsts.DefaultValues.StackTraceLimit;
		}

		protected double ParseSpanFramesMinDurationInMilliseconds(ConfigurationKeyValue kv)
		{
			string value;
			if (kv == null || string.IsNullOrWhiteSpace(kv.Value))
				value = ConfigConsts.DefaultValues.SpanFramesMinDuration;
			else
				value = kv.Value;

			double valueInMilliseconds;

			try
			{
				if (!TryParseTimeInterval(value, out  valueInMilliseconds, TimeSuffix.Ms))
				{
					Logger?.Error()
						?.Log("Failed to parse provided span frames minimum duration `{ProvidedSpanFramesMinDuration}' - " +
							"using default: {DefaultSpanFramesMinDuration}",
							value,
							ConfigConsts.DefaultValues.SpanFramesMinDuration);
					return ConfigConsts.DefaultValues.SpanFramesMinDurationInMilliseconds;
				}
			}
			catch (ArgumentException e)
			{
				Logger?.Error()
					?.LogException(e, "Failed to parse provided span frames minimum duration `{ProvidedSpanFramesMinDuration}' - " +
						"using default: {DefaultSpanFramesMinDuration}",
						value,
						ConfigConsts.DefaultValues.SpanFramesMinDuration);
				return ConfigConsts.DefaultValues.SpanFramesMinDurationInMilliseconds;
			}


			return valueInMilliseconds;
		}

		private bool TryParseTimeInterval(string valueAsString, out double valueInMilliseconds, TimeSuffix defaultSuffix)
		{
			switch (valueAsString)
			{
				case string _ when valueAsString.Length >= 2 && valueAsString.Substring(valueAsString.Length - 2).ToLowerInvariant() == "ms":
					return TryParseFloatingPoint(valueAsString.Substring(0, valueAsString.Length - 2), out valueInMilliseconds);

				case string _ when char.ToLower(valueAsString.Last()) == 's':
					if (!TryParseFloatingPoint(valueAsString.Substring(0, valueAsString.Length - 1), out var valueInSeconds))
					{
						valueInMilliseconds = 0;
						return false;
					}
					valueInMilliseconds = TimeSpan.FromSeconds(valueInSeconds).TotalMilliseconds;
					return true;

				case string _ when char.ToLower(valueAsString.Last()) == 'm':
					if (!TryParseFloatingPoint(valueAsString.Substring(0, valueAsString.Length - 1), out var valueInMinutes))
					{
						valueInMilliseconds = 0;
						return false;
					}
					valueInMilliseconds = TimeSpan.FromMinutes(valueInMinutes).TotalMilliseconds;
					return true;
				default:
					if (!TryParseFloatingPoint(valueAsString, out var valueNoUnits))
					{
						valueInMilliseconds = 0;
						return false;
					}

					switch (defaultSuffix)
					{
						case TimeSuffix.M:
							valueInMilliseconds = TimeSpan.FromMinutes(valueNoUnits).TotalMilliseconds;
							break;
						case TimeSuffix.Ms:
							valueInMilliseconds = TimeSpan.FromMilliseconds(valueNoUnits).TotalMilliseconds;
							break;
						case TimeSuffix.S:
							valueInMilliseconds = TimeSpan.FromSeconds(valueNoUnits).TotalMilliseconds;
							break;
						default:
							throw new ArgumentException( "Unexpected TimeSuffix value", nameof(defaultSuffix));
					}

					return true;
			}
		}

		protected virtual string DiscoverServiceName()
		{
			var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName();
			if (entryAssemblyName != null && !IsMsOrElastic(entryAssemblyName.GetPublicKeyToken()))
				return entryAssemblyName.Name;

			var stackFrames = new StackTrace().GetFrames();
			if (stackFrames == null) return null;

			foreach (var frame in stackFrames)
			{
				var currentAssemblyName = frame?.GetMethod()?.DeclaringType?.Assembly.GetName();
				if (currentAssemblyName != null && !IsMsOrElastic(currentAssemblyName.GetPublicKeyToken())) return currentAssemblyName.Name;
			}

			return null;
		}

		internal static string AdaptServiceName(string originalName) => originalName?.Replace('.', '_');

		protected string ParseServiceName(ConfigurationKeyValue kv)
		{
			var nameInConfig = kv.Value;
			if (!string.IsNullOrEmpty(nameInConfig))
			{
				var adaptedServiceName = AdaptServiceName(nameInConfig);
				if (nameInConfig == adaptedServiceName)
					Logger?.Warning()?.Log("Service name provided in configuration is {ServiceName}", nameInConfig);
				else
				{
					Logger?.Warning()
						?.Log("Service name provided in configuration ({ServiceNameInConfiguration}) was adapted to {ServiceName}", nameInConfig,
							adaptedServiceName);
				}
				return adaptedServiceName;
			}

			Logger?.Info()?.Log("The agent was started without a service name. The service name will be automatically discovered.");

			var discoveredName = AdaptServiceName(DiscoverServiceName());
			if (discoveredName != null)
			{
				Logger?.Info()
					?.Log("The agent was started without a service name. The automatically discovered service name is {ServiceName}", discoveredName);
				return discoveredName;
			}

			Logger?.Error()
				?.Log("Failed to discover service name, the service name will be '{DefaultServiceName}'." +
					" You can fix this by setting the service name to a specific value (e.g. by using the environment variable {ServiceNameVariable})",
					ConfigConsts.DefaultValues.UnknownServiceName, ConfigConsts.EnvVarNames.ServiceName);
			return ConfigConsts.DefaultValues.UnknownServiceName;
		}

		private static bool TryParseFloatingPoint(string valueAsString, out double result) =>
			double.TryParse(valueAsString, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

		protected double ParseTransactionSampleRate(ConfigurationKeyValue kv)
		{
			if (kv?.Value == null)
			{
				Logger?.Debug()
					?.Log("No transaction sample rate provided. Defaulting to '{DefaultTransactionSampleRate}'",
						ConfigConsts.DefaultValues.TransactionSampleRate);
				return ConfigConsts.DefaultValues.TransactionSampleRate;
			}

			if (!TryParseFloatingPoint(kv.Value, out var parsedValue))
			{
				Logger?.Error()
					?.Log("Failed to parse provided transaction sample rate `{ProvidedTransactionSampleRate}' - " +
						"using default: {DefaultTransactionSampleRate}",
						kv.Value,
						ConfigConsts.DefaultValues.TransactionSampleRate);
				return ConfigConsts.DefaultValues.TransactionSampleRate;
			}

			if (!Sampler.IsValidRate(parsedValue))
			{
				Logger?.Error()
					?.Log(
						"Provided transaction sample rate is invalid {ProvidedTransactionSampleRate} - " +
						"using default: {DefaultTransactionSampleRate}",
						parsedValue,
						ConfigConsts.DefaultValues.TransactionSampleRate);
				return ConfigConsts.DefaultValues.TransactionSampleRate;
			}

			Logger?.Debug()
				?.Log("Using provided transaction sample rate `{ProvidedTransactionSampleRate}' parsed as {ProvidedTransactionSampleRate}",
					kv.Value,
					parsedValue);
			return parsedValue;
		}

		internal static bool IsMsOrElastic(byte[] array)
		{
			var elasticToken = new byte[] { 174, 116, 0, 210, 193, 137, 207, 34 };
			var mscorlibToken = new byte[] { 183, 122, 92, 86, 25, 52, 224, 137 };
			var systemWebToken = new byte[] { 176, 63, 95, 127, 17, 213, 10, 58 };
			var systemPrivateCoreLibToken = new byte[] { 124, 236, 133, 215, 190, 167, 121, 142 };
			var msAspNetCoreHostingToken = new byte[] { 173, 185, 121, 56, 41, 221, 174, 96 };

			if (array.Length != 8)
				return false;

			var isMsCorLib = true;
			var isElasticApm = true;
			var isSystemWeb = true;
			var isSystemPrivateCoreLib = true;
			var isMsAspNetCoreHosting = true;

			for (var i = 0; i < 8; i++)
			{
				if (array[i] != elasticToken[i])
					isElasticApm = false;
				if (array[i] != mscorlibToken[i])
					isMsCorLib = false;
				if (array[i] != systemWebToken[i])
					isSystemWeb = false;
				if (array[i] != systemPrivateCoreLibToken[i])
					isSystemPrivateCoreLib = false;
				if (array[i] != msAspNetCoreHostingToken[i])
					isMsAspNetCoreHosting = false;

				if (!isMsCorLib && !isElasticApm && !isSystemWeb && !isSystemPrivateCoreLib && !isMsAspNetCoreHosting)
					return false;
			}

			return true;
		}

		private enum TimeSuffix
		{
			M,
			Ms,
			S
		}
	}
}
