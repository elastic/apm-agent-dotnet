using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TestEnvironment.Docker
{
    public class FuncContainerWaiter : BaseContainerWaiter<Container>
    {
        private readonly Func<Container, Task<bool>> _waitFunc;

        public FuncContainerWaiter(Func<Container, Task<bool>> waitFunc, ILogger logger = null)
            : base(logger)
        {
            _waitFunc = waitFunc;
        }

        protected override Task<bool> PerformCheck(Container container, CancellationToken cancellationToken) =>
            _waitFunc(container);
    }
}
