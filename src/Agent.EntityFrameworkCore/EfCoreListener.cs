using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Agent.Core;
using Elastic.Agent.Core.DiagnosticListeners;
using Elastic.Agent.Core.DiagnosticSource;

namespace Elastic.Agent.EntityFrameworkCore
{
    //TODO: probably rename, make it somehow central to start every listener with 1 line - should live in Agent.Core
    public class EfCoreListener
    {
        private Config _agentConfig = new Config(); //TODO: Config should be passed from outside

        public void Start()
        {
            System.Diagnostics.DiagnosticListener
                  .AllListeners
                  .Subscribe(new DiagnosticInitializer(new List<IDiagnosticListener>{ new EfCoreDiagnosticListener(), new HttpDiagnosticListener(_agentConfig) }));
        }
    }
}
