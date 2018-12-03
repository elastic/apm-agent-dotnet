using System;
using System.Collections.Generic;
using Elastic.Agent.Core.Logging;

namespace Elastic.Agent.Core.Config
{
    public interface IConfig
    {
        AbstractLogger Logger { get; set; }

        List<Uri> ServerUrls { get; }

        LogLevel LogLevel { get; set; }
    }
}
