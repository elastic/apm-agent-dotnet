using Elastic.Apm.Model;

namespace Elastic.Apm.Tests.Mocks
{
	internal class NoopCurrentExecutionSegmentsContainer : ICurrentExecutionSegmentsContainer
	{
		public Span CurrentSpan { get; set; }
		public Transaction CurrentTransaction { get; set; }
	}
}
