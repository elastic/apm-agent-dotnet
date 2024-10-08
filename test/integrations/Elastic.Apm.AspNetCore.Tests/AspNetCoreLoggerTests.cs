// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="ApmExtensionsLogger" /> type.
	/// </summary>
	public class AspNetCoreLoggerTests
	{
		[Fact]
		public void AspNetCoreLoggerShouldThrowExceptionWhenLoggerFactoryIsNull()
			=> Assert.Throws<ArgumentNullException>(() => new ApmExtensionsLogger(null));

		[Fact]
		public void AspNetCoreLoggerShouldGetLoggerFromFactoryWithProperCategoryName()
		{
			var loggerFactoryMock = new Mock<ILoggerFactory>();
			loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
				.Returns(() => Mock.Of<ILogger>());

			// ReSharper disable UnusedVariable
			var logger = new ApmExtensionsLogger(loggerFactoryMock.Object);
			// ReSharper restore UnusedVariable

			loggerFactoryMock.Verify(x => x.CreateLogger(It.Is<string>(s => s.Equals("Elastic.Apm"))), Times.Once);
		}
	}
}
