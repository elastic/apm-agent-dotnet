using System;
using System.Collections.Generic;
using Elastic.Agent.Core.Logging;

namespace Elastic.Agent.Core.Tests.Mocks
{
    public class TestLogger : AbstractLogger
    {
        public List<String> Lines { get; } = new List<string>();

        protected override void PrintLogline(string logline) => Lines.Add(logline);
    }
}
