// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Logging
{
	/// <summary>
	/// Performs logging for the Elastic APM agent
	/// </summary>
	public interface IApmLogger
	{
		/// <summary>
		/// Checks if the given log level is enabled
		/// </summary>
		/// <param name="level">the log level</param>
		/// <returns><c>true</c> if the log level is enabled, <c>false</c> otherwise.</returns>
		bool IsEnabled(LogLevel level);

		/// <summary>Writes a log entry.</summary>
		/// <param name="level">Entry will be written on this level.</param>
		/// <param name="state">The entry to be written. Can be also an object.</param>
		/// <param name="e">The exception related to this entry.</param>
		/// <param name="formatter">Function to create a <see cref="T:System.String" />
		/// message of the <paramref name="state" /> and <paramref name="e" />.</param>
		/// <typeparam name="TState">The type of the object to be written.</typeparam>
		void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter);
	}

	/// <summary>
	/// Has a log level that can be dynamically changed at runtime
	/// </summary>
	public interface ILogLevelSwitchable
	{
		/// <summary>
		/// A switch to dynamically control the log level at runtime
		/// </summary>
		LogLevelSwitch LogLevelSwitch { get; }
	}
}
