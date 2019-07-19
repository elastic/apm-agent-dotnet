using Elastic.Apm.Helpers;

namespace Elastic.Apm.Model
{
	internal class SpanCount
	{
		public int Dropped { get; set; }
		public int Started { get; set; }

		public override string ToString() =>
			new ToStringBuilder(nameof(SpanCount)) { { nameof(Started), Started }, { nameof(Dropped), Dropped }, }.ToString();
	}
}
