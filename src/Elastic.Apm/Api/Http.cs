using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// An object containing contextual data of the related http request.
	/// It can be attached to an <see cref="ISpan" /> through <see cref="ISpan.Context" />
	/// </summary>
	public class Http
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Method { get; set; }

		[JsonProperty("status_code")]
		public int StatusCode { get; set; }

		public string Url { get; set; }
	}
}
