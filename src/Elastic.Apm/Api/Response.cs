using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Encapsulates Response related information that can be attached to an <see cref="ITransaction" /> through <see cref="ITransaction.Context" />
	/// See <see cref="Context.Response" />
	/// </summary>
	public struct Response
	{
		public bool Finished { get; set; }

		/// <summary>
		/// The HTTP status code of the response.
		/// </summary>
		[JsonProperty("Status_code")]
		public int StatusCode { get; set; }
	}
}
