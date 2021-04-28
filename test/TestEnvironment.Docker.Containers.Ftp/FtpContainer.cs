using System.Collections.Generic;
using Docker.DotNet;
using Microsoft.Extensions.Logging;
using IP = System.Net.IPAddress;

namespace TestEnvironment.Docker.Containers.Ftp
{
    public class FtpContainer : Container
    {
        public FtpContainer(
            DockerClient dockerClient,
            string name,
            string ftpUserName,
            string ftpPassword,
            string imageName = "stilliard/pure-ftpd",
            string tag = "hardened",
            IDictionary<string, string> environmentVariables = null,
            IDictionary<ushort, ushort> ports = null,
            bool isDockerInDocker = false,
            bool reuseContainer = false,
            ILogger logger = null)
            : base(
                  dockerClient,
                  name,
                  imageName,
                  tag,
                  new Dictionary<string, string> { ["PUBLICHOST"] = IP.Loopback.ToString(), ["FTP_USER_NAME"] = ftpUserName, ["FTP_USER_PASS"] = ftpPassword, ["FTP_USER_HOME"] = $"/home/ftpusers/{ftpUserName}" }.MergeDictionaries(environmentVariables),
                  ports,
                  isDockerInDocker,
                  reuseContainer,
                  new FtpContainerWaiter(logger),
                  new FtpContainerCleaner(logger),
                  logger)
        {
            FtpUserName = ftpUserName;
            FtpPassword = ftpPassword;
        }

        public string FtpUserName { get; }

        public string FtpPassword { get; }

        public string FtpHost => IsDockerInDocker ? IPAddress : IP.Loopback.ToString();
    }
}
