using Elastic.Apm.Model;

namespace Elastic.Apm.Tests.Mocks
{
	internal class NoopCurrentExecutionSegmentsContainer : ICurrentExecutionSegmentsContainer
	{
		public Transaction CurrentTransaction { get; set; }
		public Span CurrentSpan { get; set; }
	}
}
