using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;

namespace TestEnvironment.Docker.Containers.Mail
{
    public class MailContainerWaiter : BaseContainerWaiter<MailContainer>
    {
        private readonly ushort _smtpPort;

        public MailContainerWaiter(ushort smtpPort = 1025, ILogger logger = null)
            : base(logger)
        {
            _smtpPort = smtpPort;
        }

        protected override async Task<bool> PerformCheck(MailContainer container, CancellationToken cancellationToken)
        {
            using var client = new SmtpClient();

            await client.ConnectAsync(
                container.IsDockerInDocker ? container.IPAddress : IPAddress.Loopback.ToString(),
                container.IsDockerInDocker ? _smtpPort : container.Ports[_smtpPort],
                cancellationToken: cancellationToken);

            return true;
        }
    }
}
