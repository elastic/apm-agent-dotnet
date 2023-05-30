// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// Based on .NET Tracer for Datadog APM by Datadog
// https://github.com/DataDog/dd-trace-dotnet

using System.Data;

namespace Elastic.Apm.AdoNet
{
	public static class DbCommandExtensions
	{
		public static IDbDataParameter CreateParameterWithValue(this IDbCommand command, string name, object value)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = name;
			parameter.Value = value;
			return parameter;
		}

		public static IDbDataParameter AddParameterWithValue(this IDbCommand command, string name, object value)
		{
			var parameter = CreateParameterWithValue(command, name, value);
			command.Parameters.Add(parameter);
			return parameter;
		}
	}
}
