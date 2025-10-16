// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elastic.Apm.Logging;

internal class ScopedLogger : IApmLogger
{
	public ScopedLogger(IApmLogger logger, string scope) => (Logger, Scope) = (logger, scope);

	internal ConditionalWeakTable<string, LogValuesFormatter> Formatters { get; } = new();

	public IApmLogger Logger { get; }

#if !NET || NETSTANDARD2_1
	private readonly object _lock = new();
#endif

	public string Scope { get; }

	public bool IsEnabled(LogLevel level) => Logger.IsEnabled(level);

	internal LogValuesFormatter GetOrAddFormatter(string message, IReadOnlyCollection<object> args)
	{
		if (Formatters.TryGetValue(message, out var formatter))
			return formatter;

		formatter = new LogValuesFormatter($"{{{{{{Scope}}}}}} {message}", args, Scope);
#if NET || NETSTANDARD2_1
		Formatters.AddOrUpdate(message, formatter);
		return formatter;
#else
		lock (_lock)
		{
			if (Formatters.TryGetValue(message, out var f))
				return f;
			Formatters.Add(message, formatter);
			return formatter;
		}
#endif
	}

	void IApmLogger.Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter) =>
		Logger.Log(level, state, e, formatter);
}
