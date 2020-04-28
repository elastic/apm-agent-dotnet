using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api.Kubernetes
{
	public class Pod
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Uid { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Pod)) { { nameof(Name), Name }, { nameof(Uid), Uid } }.ToString();
	}
}
