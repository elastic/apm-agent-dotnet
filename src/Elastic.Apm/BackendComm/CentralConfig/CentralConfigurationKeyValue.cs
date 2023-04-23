// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Config;

namespace Elastic.Apm.BackendComm.CentralConfig
{
	internal class CentralConfigurationKeyValue : ConfigurationKeyValue
	{
		public CentralConfigurationKeyValue(DynamicConfigurationOption option, string value, string readFrom)
			: base(option.ToConfigurationOption(), ConfigurationOrigin.CentralConfig, value, readFrom) { }
	}
}
