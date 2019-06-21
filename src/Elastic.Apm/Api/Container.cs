using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class Container
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Id { get; set; }
	}
}
