using System.Threading;
using System.Threading.Tasks;

namespace TestEnvironment.Docker
{
    public interface IContainerCleaner
    {
        /// <summary>
        /// Cleanup the dependency by removing all the data.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        Task Cleanup(Container container, CancellationToken token = default);
    }

    public interface IContainerCleaner<TContainer> : IContainerCleaner
    {
        Task Cleanup(TContainer container, CancellationToken token = default);
    }
}
