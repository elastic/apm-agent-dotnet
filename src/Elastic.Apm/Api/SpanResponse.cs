// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class SpanResponse
	{
		public Dictionary<string, string[]> Headers { get; set; }

		/// <summary>
		/// The HTTP status code of the response.
		/// </summary>
		[JsonProperty("status_code")]
		public int StatusCode { get; set; }
	}
}
