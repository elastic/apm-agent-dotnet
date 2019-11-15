using Elastic.Apm.Model;

namespace Elastic.Apm
{
	internal interface ICurrentExecutionSegmentsContainer
	{
		Span CurrentSpan { get; set; }
		Transaction CurrentTransaction { get; set; }
	}
}
