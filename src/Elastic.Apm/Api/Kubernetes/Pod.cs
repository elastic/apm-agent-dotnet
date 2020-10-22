// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api.Kubernetes
{
	public class Pod
	{
		[JsonConverter(typeof(TruncateToMaxLengthJsonConverter))]
		public string Name { get; set; }

		[JsonConverter(typeof(TruncateToMaxLengthJsonConverter))]
		public string Uid { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Pod)) { { nameof(Name), Name }, { nameof(Uid), Uid } }.ToString();
	}
}
