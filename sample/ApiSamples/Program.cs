using System;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.Model.Payload;

namespace ApiSamples
{
    class Program
    {
        static void Main(string[] args)
        {
            SampleCustomTransaction();
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
    }
}
