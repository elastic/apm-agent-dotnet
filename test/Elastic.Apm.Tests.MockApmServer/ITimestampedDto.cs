namespace Elastic.Apm.Tests.MockApmServer
{
	public interface ITimestampedDto : IDto
	{
		long Timestamp { get; }
	}
}
