using System;
using Elastic.Apm.Model;

namespace Elastic.Apm
{
	internal interface ICurrentExecutionSegmentsContainer
	{
		Transaction CurrentTransaction { get; set; }
		Span CurrentSpan { get; set; }
	}
}
