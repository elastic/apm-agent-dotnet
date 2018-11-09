using System;
using System.Collections.Generic;

namespace Elastic.Agent.Core.DiagnosticSource
{
    /// <summary>
    /// Common interface for every diagnostic listener
    /// The DiagnisticInitializer works through this interface with the different listeners
    /// </summary>
    public interface IDiagnosticListener : IObserver<KeyValuePair<string, object>>
    {
        String Name { get; }
    }
}
