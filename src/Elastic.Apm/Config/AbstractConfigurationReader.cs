using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	public abstract class AbstractConfigurationReader
	{
		protected AbstractConfigurationReader(AbstractLogger logger) => Logger = logger;

		protected AbstractLogger Logger { get; }

		protected static ConfigurationKeyValue Kv(string key, string value, string origin) =>
			new ConfigurationKeyValue(key, value, origin);

		protected internal static LogLevel ParseLogLevel(string value) =>
			Enum.TryParse<LogLevel>(value, out var logLevel) ? logLevel : AbstractLogger.LogLevelDefault;

		protected LogLevel ParseLogLevel(ConfigurationKeyValue kv)
		{
			if (kv == null || string.IsNullOrEmpty(kv.Value)) return AbstractLogger.LogLevelDefault;

			if (Enum.TryParse<LogLevel>(kv.Value, out var logLevel)) return logLevel;

			Logger.LogError("Config",
				$"Failed parsing log level from {kv.ReadFrom}: {kv.Key}, value: {kv.Value}. Defaulting to log level 'Error'");

			return AbstractLogger.LogLevelDefault;
		}

		protected IReadOnlyList<Uri> ParseServerUrls(ConfigurationKeyValue kv)
		{
			var name = GetType().Name;
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

				Logger.LogError(name, $"Failed parsing server URL from {kv.ReadFrom}: {kv.Key}, value: {u}");
			}

			return list.Count == 0 ? LogAndReturnDefault().AsReadOnly() : list.AsReadOnly();

			List<Uri> LogAndReturnDefault()
			{
				list.Add(ConfigConsts.DefaultServerUri);
				Logger.LogDebug(name, $"Using default ServerUrl: {ConfigConsts.DefaultServerUri}");
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
				Logger.LogInfo("Config", "The agent was started without a service name. The service name will be automatically calculated.");
				retVal = Assembly.GetEntryAssembly()?.GetName().Name;
			}

			if (string.IsNullOrEmpty(retVal))
			{
				var stackFrames = new StackTrace(true).GetFrames();
				foreach (var frame in stackFrames)
				{
					var currentAssembly = frame.GetMethod()?.DeclaringType.Assembly;
					var token = currentAssembly.GetName().GetPublicKeyToken();
					if (currentAssembly != null
						&& !IsMsOrElastic(currentAssembly.GetName().GetPublicKeyToken()))
					{
						retVal = currentAssembly.GetName().Name;
						break;
					}
				}
			}

			if (string.IsNullOrEmpty(retVal))
			{
				Logger.LogError("Config", "Failed calculating service name, the service name will be \'unknown\'." +
					$" You can fix this by setting the service name to a specific value (e.g. by using the environment variable {ConfigConsts.ConfigKeys.ServiceName})");
				retVal = "unknown";
			}

			return retVal.Replace('.', '_');
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
