// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;

namespace Elastic.Apm.Tests.MockApmServer
{
	public class SpanCountDto
	{
		public int Dropped { get; set; }
		public int Started { get; set; }

		public override string ToString() =>
			new ToStringBuilder(nameof(SpanCount)) { { nameof(Started), Started }, { nameof(Dropped), Dropped } }.ToString();
	}
}
