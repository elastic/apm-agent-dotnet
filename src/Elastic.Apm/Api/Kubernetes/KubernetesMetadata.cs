using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api.Kubernetes
{
	public class KubernetesMetadata
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Namespace { get; set; }

		public Node Node { get; set; }

		public Pod Pod { get; set; }
	}
}
