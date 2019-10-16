using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class System
	{
		public Container Container { get; set; }

		[JsonProperty("detected_hostname")]
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Hostname { get; set; }

		public override string ToString() =>
			new ToStringBuilder(nameof(System)) { { nameof(Container), Container }, { nameof(Hostname), Hostname } }.ToString();
	}
}
