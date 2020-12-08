// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	/// <summary>
	/// Gets Environment variables, catching and logging any exception that may be thrown.
	/// </summary>
	internal static class EnvironmentHelper
	{
		public static string GetEnvironmentVariable(string key, IApmLogger logger)
		{
			try
			{
				return Environment.GetEnvironmentVariable(key);
			}
			catch (Exception e)
			{
				logger.Debug()?.LogException(e,
					"Error while getting environment variable {EnvironmentVariable}", key);
			}

			return null;
		}

		public static IDictionary GetEnvironmentVariables(IApmLogger logger)
		{
			try
			{
				return Environment.GetEnvironmentVariables();
			}
			catch (Exception e)
			{
				logger.Debug()?.LogException(e, "Error while getting environment variables");
			}

			return null;
		}
	}
}
