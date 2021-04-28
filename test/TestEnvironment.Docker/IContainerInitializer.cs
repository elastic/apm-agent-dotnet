using System.Threading;
using System.Threading.Tasks;

namespace TestEnvironment.Docker
{
    public interface IContainerInitializer
    {
        Task<bool> Initialize(Container container, CancellationToken cancellationToken);
    }

    public interface IContainerInitializer<in TContainer> : IContainerInitializer
    {
        Task<bool> Initialize(TContainer container, CancellationToken cancellationToken);
    }
}
