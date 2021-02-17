// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Tests.Utilities
{
	internal static class EnvVarUtils
	{
		internal static bool GetBoolValue(string envVarName, bool defaultValue) => GetBoolValue(envVarName, defaultValue, out _);

		internal static bool GetBoolValue(string envVarName, bool defaultValue, out string reason)
		{
			var value = GetBoolValue(envVarName, out reason);
			if (value == null) return defaultValue;

			return value.Value;
		}

		// ReSharper disable once MemberCanBePrivate.Global
		internal static bool? GetBoolValue(string envVarName, out string reason)
		{
			var envVarValue = Environment.GetEnvironmentVariable(envVarName);
			if (envVarValue == null)
			{
				reason = $"environment variable {envVarName} is not set";
				return null;
			}

			reason = $"environment variable {envVarName} is set to `{envVarValue}'";
			if (!bool.TryParse(envVarValue, out var envVarBoolValue))
			{
				reason += " which is not a valid boolean value";
				return null;
			}

			return envVarBoolValue;
		}
	}
}
