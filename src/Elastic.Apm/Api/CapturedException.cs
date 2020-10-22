// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class CapturedException
	{
		[JsonConverter(typeof(TruncateToMaxLengthJsonConverter))]
		public string Code { get; set; }

		public bool Handled { get; set; }

		public string Message { get; set; }

		[JsonProperty("stacktrace")]
		public List<CapturedStackFrame> StackTrace { get; set; }

		[JsonConverter(typeof(TruncateToMaxLengthJsonConverter))]
		public string Type { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(CapturedException))
		{
			{ nameof(Type), Type }, { nameof(Message), Message }, { nameof(Handled), Handled }, { nameof(Code), Code }
		}.ToString();
	}
}
