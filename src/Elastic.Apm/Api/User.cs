// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class User
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Email { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Id { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("username")]
		public string UserName { get; set; }

		public override string ToString() =>
			new ToStringBuilder(nameof(User)) { { "Id", Id }, { "UserName", UserName }, { "Email", Email } }.ToString();
	}
}
