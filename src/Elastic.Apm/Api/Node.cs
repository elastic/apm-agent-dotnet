using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class Node
	{
		[JsonProperty("configured_name")]
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string ConfiguredName { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Service)) { { nameof(ConfiguredName), ConfiguredName } }.ToString();
	}
}
