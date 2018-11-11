using System;
namespace Elastic.Agent.Core
{
    public class Config
    {
        public Uri ServerUri { get; set; } = new Uri("http://127.0.0.1:8200"); //TODO: read from config
    }
}
