using Elastic.Apm.Helpers;
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

		public override string ToString() =>
			new ToStringBuilder(nameof(KubernetesMetadata)) { { nameof(Namespace), Namespace }, { nameof(Node), Node }, { nameof(Pod), Pod } }
				.ToString();
	}
}
