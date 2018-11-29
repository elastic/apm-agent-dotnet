using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Agent.Core;
using Elastic.Agent.Core.DiagnosticListeners;
using Elastic.Agent.Core.DiagnosticSource;

namespace Elastic.Agent.EntityFrameworkCore
{
    /// <summary>
    /// Manages the Entity Framework Core listener, which listenes to EF Core events
    /// </summary>
    public class ElasticEntityFrameworkCoreListener
    {
        /// <summary>
        /// Start listening for EF Core diagnosticsource events
        /// </summary>
        public void Start()
        {
                DiagnosticListener
                  .AllListeners
                  .Subscribe(new DiagnosticInitializer(new List<IDiagnosticListener>{ new EfCoreDiagnosticListener()}));
        }
    }
}
