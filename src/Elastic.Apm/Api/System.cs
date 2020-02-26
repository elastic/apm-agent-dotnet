using Elastic.Apm.Api.Kubernetes;
using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class System
	{
		public KubernetesMetadata Kubernetes { get; set; }

		public Container Container { get; set; }

		[JsonProperty("detected_hostname")]
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string DetectedHostName { get; set; }

		[JsonProperty("hostname")]
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string HostName => DetectedHostName;

		public override string ToString() =>
			new ToStringBuilder(nameof(System)) { { nameof(Container), Container }, { nameof(DetectedHostName), DetectedHostName } }.ToString();
	}
}
