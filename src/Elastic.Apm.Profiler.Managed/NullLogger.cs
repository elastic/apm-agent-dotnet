// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Profiler.Managed
{
	// TODO: Replace with a *real* logger
	public class NullLogger
	{
		public static NullLogger Instance { get; } = new NullLogger();

		public void Debug(string message) => Log("DEBUG", message);

		public void Error(Exception exception, string message) => Log("ERROR", message, exception);

		public void Warning(string message) => Log("WARN", message);

		private void Log(string level, string message, Exception exception = null)
		{
			//Console.WriteLine($"[{level}] {message}. {ex}");
		}
	}
}
