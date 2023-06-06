// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Config
{
	/// <summary>
	/// A snapshot of agent configuration containing values
	/// initial configuration combined with dynamic values from central configuration, if enabled.
	/// </summary>
	public interface IConfiguration : IConfigurationReader
	{
	}

}
