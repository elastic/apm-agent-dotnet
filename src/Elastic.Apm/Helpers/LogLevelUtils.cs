// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	internal static class LogLevelUtils
	{
		internal static LogLevel GetFinest(LogLevel logLevel1, LogLevel logLevel2) =>
			logLevel1.CompareTo(logLevel2) <= 0 ? logLevel1 : logLevel2;
	}
}
