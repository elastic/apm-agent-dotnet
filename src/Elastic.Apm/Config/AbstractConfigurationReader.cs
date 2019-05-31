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

		protected internal static bool TryParseLogLevel(string value, out LogLevel? level)
		{
			level = null;
			if (string.IsNullOrEmpty(value)) return false;

			level = DefaultLogLevel();
			return level != null;

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
			if (bool.TryParse(kv.Value, out var value)) return value;

			return true;
		}

		protected LogLevel ParseLogLevel(ConfigurationKeyValue kv)
		{
			if (TryParseLogLevel(kv?.Value, out var level)) return level.Value;

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
				list.Add(ConfigConsts.DefaultServerUri);
				Logger?.Debug()?.Log("Using default ServerUrl: {ServerUrl}", ConfigConsts.DefaultServerUri);
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
			{
				value = ConfigConsts.DefaultValues.MetricsInterval;
			}
			else
			{
				value = kv.Value;
			}

			double doubleVal;

			switch (value)
			{
				case string str when str.Length >= 2 && str.Substring(str.Length-2).ToLower() == "ms":
					if (double.TryParse(str.Substring(0, str.Length - 2), out doubleVal) && doubleVal >= 0)
						// ReSharper disable once CompareOfFloatsByEqualityOperator - we compare to exactly zero here
						return  doubleVal < 1000 && doubleVal != 0 ? 0 : doubleVal;
					break;
				case string str when char.ToLower(str.Last())== 's':
					if (double.TryParse(str.Substring(0, str.Length - 1), out doubleVal) && doubleVal >= 0)
						// ReSharper disable once CompareOfFloatsByEqualityOperator - we compare to exactly zero here
						return  doubleVal < 1 && doubleVal != 0 ? 0 : doubleVal * 1000;
					break;
				case string str when char.ToLower(str.Last()) == 'm':
					if (double.TryParse(str.Substring(0, str.Length - 1), out doubleVal) && doubleVal >= 0)
						// ReSharper disable once CompareOfFloatsByEqualityOperator - we compare to exactly zero here
						return  doubleVal < 0.016666666666667 && doubleVal != 0 ? 0 : doubleVal * 1000 * 60;
					break;
			}

			if (double.TryParse(value, out doubleVal) && doubleVal >= 0)
				return doubleVal * 1000;

			return 30 * 1000;
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
				var currentAssemblyName = frame?.GetMethod()?.DeclaringType?.Assembly?.GetName();
				if (currentAssemblyName != null && !IsMsOrElastic(currentAssemblyName.GetPublicKeyToken())) return currentAssemblyName.Name;
			}

			return null;
		}

		private string AdaptServiceName(string originalName) => originalName?.Replace('.', '_');

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
	}
}
