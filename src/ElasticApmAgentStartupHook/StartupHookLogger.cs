// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;

namespace ElasticApmStartupHook
{
	/// <summary>
	/// Logs startup hook process, useful for debugging purposes.
	/// </summary>
	public class StartupHookLogger
	{
		private readonly bool _enabled;
		private readonly string _logPath;

		private StartupHookLogger(string logPath, bool enabled)
		{
			_logPath = logPath;
			_enabled = enabled;
		}

		/// <summary>
		/// Returns a logger and initializes it based on environment variables
		/// </summary>
		/// <returns></returns>
		public static StartupHookLogger Create()
		{
			var startupHookEnvVar = Environment.GetEnvironmentVariable("DOTNET_STARTUP_HOOKS");

			var startupHookDirectory = Path.GetDirectoryName(startupHookEnvVar);
			var startupHookLoggingEnvVar = Environment.GetEnvironmentVariable("ELASTIC_APM_STARTUP_HOOKS_LOGGING");

			return new StartupHookLogger(Path.Combine(startupHookDirectory, "ElasticApmAgentStartupHook.log"),
				!string.IsNullOrEmpty(startupHookLoggingEnvVar));
		}

		public void WriteLine(string message)
		{
			if (_enabled)
			{
				try
				{
					var log = $"[{DateTime.Now:u}] {message}";
					Console.Out.WriteLine(log);
					Console.Out.Flush();
					File.AppendAllLines(_logPath, new[] { log });
				}
				catch
				{
					// if we can't log a log message, there's not much that can be done, so ignore
				}
			}
		}
	}
}
