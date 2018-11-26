using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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

        /// <summary>
        /// Calls HttpDiagnosticListener.OnNext with types that are unknown.
        /// The test makes sure that in this case still no exception is thrown from the OnNext method.
        /// </summary>
        [Fact]
        public void UnknownObjectToOnNext()
        {           
            var listener = new HttpDiagnosticListener(new Config());
            var myFake = new StringBuilder(); //just a random type that is not planned to be passed into OnNext

            var exception = 
                Record.Exception(() =>
                {
                    listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start", myFake));
                });

            Assert.Null(exception);
        }

        /// <summary>
        /// Passes null instead of a valid HttpRequestMessage into HttpDiagnosticListener.OnNext
        /// and makes sure that still no exception is thrown
        /// </summary>
        [Fact]
        public void NullValueToOnNext()
        {
            var listener = new HttpDiagnosticListener(new Config());

            var exception =
                Record.Exception(() =>
                {
                    listener.OnNext(new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start", null));
                });

            Assert.Null(exception);
        }

        /// <summary>
        /// Passes a null key with null value instead of a valid HttpRequestMessage into HttpDiagnosticListener.OnNext
        /// and makes sure that still no exception is thrown
        /// </summary>
        [Fact]
        public void NullKeyValueToOnNext()
        {
            var listener = new HttpDiagnosticListener(new Config());

            var exception =
                Record.Exception(() =>
                {
                    listener.OnNext(new KeyValuePair<string, object>(null, null));
                });

            Assert.Null(exception);
        }

        /// <summary>
        /// Sends a simple HTTP GET message and makes sure that 
        /// HttpDiagnosticListener captures it
        /// </summary>
        /// <returns></returns>
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
                var res = await httpClient.GetAsync(localServer.Uri);

                Assert.True(res.IsSuccessStatusCode);
                Assert.Equal(localServer.Uri, TransactionContainer.Transactions.Value[0].Spans[0].Context.Http.Url);
            }

            Assert.Equal(200, TransactionContainer.Transactions.Value[0].Spans[0].Context.Http.Status_code);
            Assert.Equal(HttpMethod.Get.ToString(), TransactionContainer.Transactions.Value[0].Spans[0].Context.Http.Method);
        }
    }
}
