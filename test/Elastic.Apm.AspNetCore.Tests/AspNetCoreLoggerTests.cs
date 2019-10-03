using System;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="Elastic.Apm.AspNetCore.AspNetCoreLogger" /> type.
	/// </summary>
	public class AspNetCoreLoggerTests
	{
		[Fact]
		public void AspNetCoreLoggerShouldThrowExceptionWhenLoggerFactoryIsNull()
			=> Assert.Throws<ArgumentNullException>(() => new AspNetCoreLogger(null));

		[Fact]
		public void AspNetCoreLoggerShouldGetLoggerFromFactoryWithProperCategoryName()
		{
			var loggerFactoryMock = new Mock<ILoggerFactory>();
			loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
				.Returns(() => Mock.Of<ILogger>());

			// ReSharper disable UnusedVariable
			var logger = new AspNetCoreLogger(loggerFactoryMock.Object);
			// ReSharper restore UnusedVariable

			loggerFactoryMock.Verify(x => x.CreateLogger(It.Is<string>(s => s.Equals("Elastic.Apm"))), Times.Once);
		}
	}
}
