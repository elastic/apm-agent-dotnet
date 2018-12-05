using System;
using System.Net;
using System.Threading.Tasks;

namespace Elastic.Apm.Tests.Mocks
{
    public class LocalServer : IDisposable
    {
        HttpListener httpListener = new HttpListener();
        public string Uri => "http://localhost:8082/";

        public LocalServer(Action<HttpListenerContext> testAction = null)
        {
            httpListener.Prefixes.Add(Uri);
            httpListener.Start();

            Task.Run(() =>
            {
                var context = httpListener.GetContext();

                if (testAction == null)
                {
                    context.Response.StatusCode = 200;
                }
                else
                {
                    testAction(context);
                }

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
