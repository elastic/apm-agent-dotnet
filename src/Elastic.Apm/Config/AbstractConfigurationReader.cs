using System;
using System.Collections.Generic;
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

			Logger?.LogError("Config",
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

				Logger?.LogError(name, $"Failed parsing server URL from {kv.ReadFrom}: {kv.Key}, value: {u}");
			}

			return list.Count == 0 ? LogAndReturnDefault().AsReadOnly() : list.AsReadOnly();

			List<Uri> LogAndReturnDefault()
			{
				list.Add(ConfigConsts.DefaultServerUri);
				Logger?.LogDebug(name, $"Using default ServerUrl: {ConfigConsts.DefaultServerUri}");
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
	}
}
