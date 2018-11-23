using System;
using System.Linq;
using Elastic.Agent.Core.DiagnosticListeners;
using Elastic.Agent.Core.Tests.Mocks;
using Xunit;

namespace Elastic.Agent.Core.Tests
{
    public class UnitTest1
    {
        /// <summary>
        /// Calls the OnError method on the HttpDiagnosticListener and makes sure that the correct error message is logged 
        /// </summary>
        [Fact]
        public void TestOnErrorLog()
        {
            Apm.Agent.SetLoggerType<TestLogger>();
            var listener = new HttpDiagnosticListener(new Config());

            var exceptionMessage = "Ooops, this went wrong";
            var fakeException = new Exception(exceptionMessage);
            listener.OnError(fakeException);

            Assert.Equal($"Error {listener.Name}: Exception in OnError, Exception-type:{nameof(Exception)}, Message:{exceptionMessage}", (listener.Logger as TestLogger)?.Lines?.FirstOrDefault());
        }
    }
}
