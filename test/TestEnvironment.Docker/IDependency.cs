using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestEnvironment.Docker
{
    /// <summary>
    /// Represents test environment dependency.
    /// </summary>
    public interface IDependency : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Gets the dependency name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Run the dependency.
        /// </summary>
        /// <param name="environmentVariables">Environment vars.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Awaitable task.</returns>
        Task Run(IDictionary<string, string> environmentVariables, CancellationToken token = default);

        /// <summary>
        /// Stop the dependency.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Awaitable task.</returns>
        Task Stop(CancellationToken token = default);
    }
}
