using System;
using System.Collections.Generic;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
    public interface IConfig
    {
        AbstractLogger Logger { get; set; }

        List<Uri> ServerUrls { get; }

        LogLevel LogLevel { get; set; }
    }
}
