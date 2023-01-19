// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	internal static class EnvironmentVariablesExtensions
	{
		internal static string SafeGetValue(this IEnvironmentVariables environmentVariables, string name)
		{
			string value = null;
			var variables = environmentVariables.GetEnvironmentVariables();
			if (variables != null)
				value = variables[name]?.ToString();
			return value ?? string.Empty;
		}

		internal static bool SafeCheckValue(this IEnvironmentVariables environmentVariables, string name,
			string value)
			=> SafeGetValue(environmentVariables, name).Equals(value, StringComparison.InvariantCultureIgnoreCase);

		internal static bool SafeCheckExists(this IEnvironmentVariables environmentVariables, string name)
			=> !string.IsNullOrEmpty(SafeGetValue(environmentVariables, name));
	}

	internal interface IEnvironmentVariables
	{
		IDictionary GetEnvironmentVariables();
	}

	internal sealed class EnvironmentVariables : IEnvironmentVariables
	{
		private readonly IApmLogger _logger;
		public EnvironmentVariables(IApmLogger logger) => _logger = logger.Scoped(nameof(EnvironmentVariables));

		public IDictionary GetEnvironmentVariables()
		{
			try
			{
				return Environment.GetEnvironmentVariables();
			}
			catch (Exception e)
			{
				_logger.Error()?.LogException(e, "could not get environment variables");
				return null;
			}
		}
	}
}
