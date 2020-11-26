using System;
using Moq;
using StackExchange.Redis;
using StackExchange.Redis.Profiling;
using Xunit;

namespace Elastic.Apm.StackExchange.Redis.Tests
{
	public class ConnectionMultiplexerExtensionTests
	{
		[Fact]
		public void UseElasticApm_Registers_ElasticApmProfiler()
		{
			var connection = new Mock<IConnectionMultiplexer>();

			connection.Object.UseElasticApm();

			connection.Verify(x =>
				x.RegisterProfiler(It.IsAny<Func<ProfilingSession>>()), Times.Once);
		}
	}
}
