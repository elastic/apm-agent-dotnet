using Newtonsoft.Json;

namespace Elastic.Apm.Model.Payload
{
	public class Response
	{
		public bool Finished { get; set; }

		[JsonProperty("Status_code")]
		public int StatusCode { get; set; }
	}
}
