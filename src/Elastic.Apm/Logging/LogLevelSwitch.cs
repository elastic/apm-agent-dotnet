// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Logging
{
	/// <summary>
	/// Dynamically controls the log level
	/// </summary>
	public class LogLevelSwitch
	{
		private volatile LogLevel _level;

		public LogLevelSwitch(LogLevel level) => _level = level;

		/// <summary>
		/// Gets or sets the current log level
		/// </summary>
		public LogLevel Level
		{
			get => _level;
			set => _level = value;
		}
	}
}
