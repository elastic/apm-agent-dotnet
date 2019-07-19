using Elastic.Apm.Model;

namespace Elastic.Apm
{
	internal interface ICurrentExecutionSegmentHolder
	{
		Transaction CurrentTransactionInternal { get; set; }
//		Span CurrentSpanInternal { get; set; }
	}
}
