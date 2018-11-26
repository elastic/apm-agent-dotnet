using System;
using System.Net;
using System.Threading.Tasks;

namespace Elastic.Agent.Core.Tests.Mocks
{
    public class LocalServer : IDisposable
    {
        HttpListener httpListener = new HttpListener();
        public string Uri => "http://localhost:8082/";

        public LocalServer()
        {
            httpListener.Prefixes.Add(Uri);
            httpListener.Start();

            Task.Run(() =>
            {
                var context = httpListener.GetContext();
                context.Response.StatusCode = 200;
                context.Response.OutputStream.Close();
                context.Response.Close();
            });
        }

        public void Dispose()
        {
            this.httpListener.Abort();
            ((IDisposable)this.httpListener).Dispose();
        }
    }
}
