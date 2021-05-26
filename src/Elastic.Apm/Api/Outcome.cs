// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.Serialization;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Libraries.Newtonsoft.Json.Converters;

namespace Elastic.Apm.Api
{
	[JsonConverter(typeof(StringEnumConverter))]
	public enum Outcome
	{
		[EnumMember(Value = "unknown")]
		Unknown = 0, //Make sure Unknown remains the default value

		[EnumMember(Value = "success")]
		Success,

		[EnumMember(Value = "failure")]
		Failure
	}
}
