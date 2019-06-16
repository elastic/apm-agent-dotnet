namespace Elastic.Apm.Tests.MockApmServer
{
	public interface ITimedDto: IDto
	{
		long Timestamp { get; }
		double Duration { get; }
	}
}
