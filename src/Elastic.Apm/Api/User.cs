using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class User
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Id { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("username")]
		public string UserName { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Email { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(User))
		{
			{ "Id", Id },
			{ "UserName", UserName },
			{ "Email", Email },
		}.ToString();
	}
}
