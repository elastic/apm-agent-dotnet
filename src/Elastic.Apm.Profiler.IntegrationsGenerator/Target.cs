// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Apm.Profiler.IntegrationsGenerator
{
	public class Target
	{
		[YamlIgnore]
		public string Nuget { get; set; }
		public string Assembly { get; set; }
		public string Type { get; set; }
		public string Method { get; set; }
		public string[] SignatureTypes { get; set; }
		public string MinimumVersion { get; set; }
		public string MaximumVersion { get; set; }
	}
}
