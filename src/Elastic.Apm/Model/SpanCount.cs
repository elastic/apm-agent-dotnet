using System;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Model
{
	internal class SpanCount
	{
		public int Started { get; set; }
		public int Dropped { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(SpanCount))
		{
			{ "Started", Started },
			{ "Dropped", Dropped },
		}.ToString();
	}
}
