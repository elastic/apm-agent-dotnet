using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Agent.Core.DiagnosticListeners;
using Elastic.Agent.Core.DiagnosticSource;
using Elastic.Agent.Core.Model.Payload;
using Elastic.Agent.Core.Tests.Mocks;
using Xunit;

namespace Elastic.Agent.Core.Tests
{
    public class HttpDiagnosticListenerTest
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


        [Fact]
        public async Task TestSimpleOutgoingHttpRequest()
        {
            new ElasticCoreListeners().Start();

            TransactionContainer.Transactions.Value = new List<Transaction>()
            {
                new Transaction()
                {
                    Name = $"{nameof(TestSimpleOutgoingHttpRequest)}",
                    Id = Guid.NewGuid(),
                    Type = "request",
                    TimestampInDateTime = DateTime.UtcNow,
                }
            };

            using (LocalServer localServer = new LocalServer())
            {
                var httpClient = new HttpClient();
                var res = await httpClient.GetAsync("http://localhost:8082");

                Assert.True(res.IsSuccessStatusCode);
                Assert.Equal(localServer.Uri, TransactionContainer.Transactions.Value[0].Spans[0].Context.Http.Url);
            }

            Assert.Equal(200, TransactionContainer.Transactions.Value[0].Spans[0].Context.Http.Status_code);
            Assert.Equal("GET", TransactionContainer.Transactions.Value[0].Spans[0].Context.Http.Method);
        }
    }
}
