using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api.Kubernetes
{
	public class Node
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Node)) { { nameof(Name), Name } }.ToString();
	}
}
