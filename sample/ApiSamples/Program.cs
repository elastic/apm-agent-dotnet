using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Model.Payload;

namespace ApiSamples
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                SampleCustomTransactionWithConvinientApi().Wait();
            }
            catch{}
            //WIP: if the process terminates the agent
            //potentially does not have time to send the transaction to the server.
            Thread.Sleep(1000);
        }

        public static void SampleCustomTransaction()
        {
            Console.WriteLine($"{nameof(SampleCustomTransaction)} started");
            var transaction = ElasticApm.StartTransaction("SampleTransaction", Transaction.TYPE_REQUEST);

            Thread.Sleep(500); //simulate work...
           
            transaction.End();
            Console.WriteLine($"{nameof(SampleCustomTransaction)} finished");
        }

        public static void SampleCustomTransactionWithSpan()
        {
            Console.WriteLine($"{nameof(SampleCustomTransactionWithSpan)} started");
            var transaction = ElasticApm.StartTransaction("SampleTransactionWithSpan", Transaction.TYPE_REQUEST);

            Thread.Sleep(500);

            var span = transaction.StartSpan("SampleSpan", Span.TYPE_EXTERNAL);
            Thread.Sleep(200);
            span.End();

            transaction.End();
            Console.WriteLine($"{nameof(SampleCustomTransactionWithSpan)} finished");
        }

        public static void SampleError()
        {
            Console.WriteLine($"{nameof(SampleError)} started");
            var transaction = ElasticApm.StartTransaction("SampleError", Transaction.TYPE_REQUEST);

            Thread.Sleep(500); //simulate work...
            var span = transaction.StartSpan("SampleSpan", Span.TYPE_EXTERNAL);
            try
            {
                throw new Exception("bamm");
            }
            catch (Exception e)
            {
                span.CaptureException(e);
            }
            finally
            {
                span.End();
            }

            transaction.End();

            Console.WriteLine($"{nameof(SampleError)} finished");
        }

        public static async Task SampleCustomTransactionWithConvinientApi()
        {
            await  ElasticApm.CaptureTransaction("transaction1", "type1", async() =>
           {
               await Task.Delay(100);
               throw new Exception("bamm!");
               return 4;
            });
        }
    }
}
