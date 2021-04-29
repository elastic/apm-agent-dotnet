// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Encapsulates Response related information that can be attached to an <see cref="ITransaction" /> through
	/// <see cref="ITransaction.Context" />
	/// See <see cref="Context.Response" />
	/// </summary>
	public class Response
	{
		public bool Finished { get; set; }

		public Dictionary<string, string> Headers { get; set; }

		/// <summary>
		/// The HTTP status code of the response.
		/// </summary>
		[JsonProperty("status_code")]
		public int StatusCode { get; set; }

		internal Response DeepCopy()
		{
			var newItem = (Response)MemberwiseClone();
			if(Headers != null)
				newItem.Headers = Headers.ToDictionary(entry => entry.Key, entry => entry.Value);
			return newItem;
		}
	}
}
