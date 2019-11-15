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
