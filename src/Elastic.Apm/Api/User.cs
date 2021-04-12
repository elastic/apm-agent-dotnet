// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Helpers;
using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class User
	{
		[MaxLength]
		public string Email { get; set; }

		[MaxLength]
		public string Id { get; set; }

		[MaxLength]
		[JsonProperty("username")]
		public string UserName { get; set; }

		public override string ToString() =>
			new ToStringBuilder(nameof(User)) { { nameof(Id), Id }, { nameof(UserName), UserName }, { nameof(Email), Email } }.ToString();
	}
}
