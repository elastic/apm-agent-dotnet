// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Config
{
	public class ConfigurationKeyValue
	{
		public ConfigurationKeyValue(string key, string value, string readFrom) =>
			(Key, Value, ReadFrom) = (key, value, readFrom);

		public string Key { get; }
		public string ReadFrom { get; }
		public string Value { get; }

		public override string ToString() => $"{Key} : {Value} ({ReadFrom})";
	}
}
