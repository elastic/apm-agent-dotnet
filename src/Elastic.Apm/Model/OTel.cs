// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;

namespace Elastic.Apm.Model
{
	/// <summary>
	/// OTel contains unmapped OpenTelemetry attributes.
	/// </summary>
	public class OTel
	{
		/// <summary>
		/// Attributes hold the unmapped OpenTelemetry attributes.
		/// </summary>
		public Dictionary<string, string> Attributes { get; set; }

		/// <summary>
		/// SpanKind holds the incoming OpenTelemetry span kind.
		/// </summary>
		public string SpanKind { get; set; }
	}
}
