// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Elastic.Apm.Logging
{
	internal class ScopedLogger : IApmLogger
	{
		public ScopedLogger(IApmLogger logger, string scope) => (Logger, Scope) = (logger, scope);

		private ConcurrentDictionary<string, LogValuesFormatter> Formatters { get; } = new ConcurrentDictionary<string, LogValuesFormatter>();

		public IApmLogger Logger { get; }

		public string Scope { get; }

		public bool IsEnabled(LogLevel level) => Logger.IsEnabled(level);

		internal LogValuesFormatter GetOrAddFormatter(string message, IReadOnlyCollection<object> args)
			=> Formatters.GetOrAdd(message, s => new LogValuesFormatter($"{{{{{{Scope}}}}}} {s}", args, Scope));

		void IApmLogger.Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter) =>
			Logger.Log(level, state, e, formatter);
	}
}
