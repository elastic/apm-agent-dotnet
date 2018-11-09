using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Agent.Core.DiagnosticSource;

namespace Elastic.Agent.EntityFrameworkCore
{
    //TODO: probably rename
    public class EfCoreListener
    {
        public void Start()
        {

            System.Diagnostics.DiagnosticListener.AllListeners.Subscribe(new DiagnosticInitializer(new EfCoreDiagnosticListener()));

        }

    }
}
