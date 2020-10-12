// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api.Kubernetes;
using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class System
	{
		private string _hostName;
		public KubernetesMetadata Kubernetes { get; set; }

		public Container Container { get; set; }

		[JsonProperty("detected_hostname")]
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string DetectedHostName { get; set; }

		[JsonProperty("hostname")]
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string HostName
		{
			get => _hostName ??= DetectedHostName;
			set => _hostName = value;
		}

		public override string ToString() =>
			new ToStringBuilder(nameof(System))
			{
				{ nameof(Container), Container },
				{ nameof(DetectedHostName), DetectedHostName },
				{ nameof(HostName), HostName }
			}.ToString();
	}
}
