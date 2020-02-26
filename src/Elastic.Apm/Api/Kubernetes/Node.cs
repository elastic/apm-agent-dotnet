using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api.Kubernetes
{
	public class Node
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }
	}
}
