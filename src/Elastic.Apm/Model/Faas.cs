// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Model
{
	/// <summary>
	/// Contains data related to FaaS (Function as a Service) events.
	/// </summary>
	public class Faas
	{
		/// <summary>
		/// Indicates whether a function invocation was a cold start or not.
		/// </summary>
		public bool ColdStart { get; set; }

		///
		/// The request id of the function invocation.
		///
		public string Execution { get; set; }

		/// <summary>
		/// A unique identifier of the invoked serverless function.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// The function name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The function version.
		/// </summary>
		public string Version { get; set; }

		/// <summary>
		/// Trigger attributes.
		/// </summary>
		public Trigger Trigger { get; set; }

		public override string ToString() => $"{Name} ({Id})";
	}

	/// <summary>
	/// Trigger attributes.
	/// </summary>
	public struct Trigger
	{
		public const string TypeOther = "other";
		public const string TypeHttp = "http";

		/// <summary>
		/// The id of the origin trigger request.
		/// </summary>
		[JsonProperty("request_id")]
		public string RequestId { get; set; }

		/// <summary>
		/// The trigger type.
		/// </summary>
		public string Type { get; set; }

		public override string ToString() => Type;
	}
}
