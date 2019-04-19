using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Elastic.Apm.Api;
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

		protected string ParseServiceName(ConfigurationKeyValue kv)
		{
			var retVal = kv.Value;
			if (string.IsNullOrEmpty(retVal))
			{
				Logger?.Info()?.Log("The agent was started without a service name. The service name will be automatically calculated.");
				retVal = Assembly.GetEntryAssembly()?.GetName().Name;
				Logger?.Info()?.Log("The agent was started without a service name. The Service name is {ServiceName}", retVal);
			}

			if (string.IsNullOrEmpty(retVal))
			{
				var stackFrames = new StackTrace().GetFrames();
				if (stackFrames != null)
				{
					foreach (var frame in stackFrames)
					{
						var currentAssembly = frame?.GetMethod()?.DeclaringType?.Assembly;
						if (currentAssembly == null || IsMsOrElastic(currentAssembly.GetName().GetPublicKeyToken())) continue;

						retVal = currentAssembly.GetName().Name;
						break;
					}
				}
			}

			if (!string.IsNullOrEmpty(retVal)) return retVal.Replace('.', '_');

			Logger?.Error()
				?.Log("Failed calculating service name, the service name will be 'unknown'." +
					" You can fix this by setting the service name to a specific value (e.g. by using the environment variable {ServiceNameVariable})",
					ConfigConsts.EnvVarNames.ServiceName);

			return "unknown";
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

			if (array.Length != 8)
				return false;

			var isMsCorLib = true;
			var isElasticApm = true;
			for (var i = 0; i < 8; i++)
			{
				if (array[i] != elasticToken[i])
					isElasticApm = false;
				if (array[i] != mscorlibToken[i])
					isMsCorLib = false;

				if (!isMsCorLib && !isElasticApm)
					return false;
			}

			return true;
		}
	}
}
