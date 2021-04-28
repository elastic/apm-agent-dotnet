using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TestEnvironment.Docker
{
    public abstract class BaseContainerWaiter<TContainer> : IContainerWaiter<TContainer>
        where TContainer : Container
    {
        protected BaseContainerWaiter(ILogger logger)
        {
            Logger = logger;
        }

        protected ILogger Logger { get; }

        protected virtual int AttemptsCount => 60;

        protected virtual TimeSpan DelayTime => TimeSpan.FromSeconds(1);

        public async Task<bool> Wait(TContainer container, CancellationToken cancellationToken)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var attempts = AttemptsCount;
            do
            {
                try
                {
                    Logger?.LogInformation($"{container.Name}: checking container state...");
                    var isAlive = await PerformCheck(container, cancellationToken);

                    if (isAlive)
                    {
                        Logger?.LogInformation($"{container.Name}: container is Up!");
                        return true;
                    }
                }
                catch (Exception exception) when (IsRetryable(exception))
                {
                    Logger?.LogError(exception, $"{container.Name} check failed with exception {exception.Message}");
                }

                attempts--;
                await Task.Delay(DelayTime, cancellationToken);
            }
            while (attempts != 0);

            Logger?.LogError($"Container {container.Name} didn't start.");
            return false;
        }

        public Task<bool> Wait(Container container, CancellationToken cancellationToken) =>
            Wait(container as TContainer, cancellationToken);

        protected abstract Task<bool> PerformCheck(TContainer container, CancellationToken cancellationToken);

        protected virtual bool IsRetryable(Exception exception) => true;
    }
}
