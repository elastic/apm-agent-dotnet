// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace ElasticApmStartupHook
{
	internal class ElasticApmAssemblyLoadContext : AssemblyLoadContext
	{
		protected override Assembly Load(AssemblyName assemblyName) => null;
	}

	/// <summary>
	/// Logs startup hook process, useful for debugging purposes.
	/// </summary>
	internal class StartupHookLogger
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
			var config = GlobalLogConfiguration.FromEnvironment(Environment.GetEnvironmentVariables());
			var path = config.CreateLogFileName("startup_hook");

			return new StartupHookLogger(path, config.IsActive);
		}

		public void WriteLine(string message)
		{
			if (!_enabled)
				return;

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
