using System;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Microsoft.Extensions.Logging;

namespace TestEnvironment.Docker.Containers.Ftp
{
    public class FtpContainerCleaner : IContainerCleaner<FtpContainer>
    {
        private readonly ILogger _logger;

        public FtpContainerCleaner(ILogger logger = null)
        {
            _logger = logger;
        }

        public async Task Cleanup(FtpContainer container, CancellationToken token = default)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            using (var ftpClient = new FtpClient(container.FtpHost, container.IsDockerInDocker ? 21 : container.Ports[21], container.FtpUserName, container.FtpPassword))
            {
                try
                {
                    await ftpClient.ConnectAsync(token);
                    foreach (var item in await ftpClient.GetListingAsync("/"))
                    {
                        if (item.Type == FtpFileSystemObjectType.Directory)
                        {
                            await ftpClient.DeleteDirectoryAsync(item.FullName, token);
                        }
                        else if (item.Type == FtpFileSystemObjectType.File)
                        {
                            await ftpClient.DeleteFileAsync(item.FullName, token);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogInformation($"Cleanup issue: {e.Message}");
                }
            }
        }

        public Task Cleanup(Container container, CancellationToken token = default) => Cleanup((FtpContainer)container, token);
    }
}
