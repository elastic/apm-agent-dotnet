namespace Elastic.Apm.Tests.MockApmServer
{
	public interface ITimedDto : ITimestampedDto
	{
		double Duration { get; }
	}
}
