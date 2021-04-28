using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestEnvironment.Docker
{
    /// <summary>
    /// Represents interface for test environment.
    /// </summary>
    public interface ITestEnvironment : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Gets the environment name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the environment variables.
        /// </summary>
        IDictionary<string, string> Variables { get; }

        /// <summary>
        /// Gets the list of environment dependencies.
        /// </summary>
        IDependency[] Dependencies { get; }

        Task Up(CancellationToken token = default);

        Task Down(CancellationToken token = default);
    }
}
