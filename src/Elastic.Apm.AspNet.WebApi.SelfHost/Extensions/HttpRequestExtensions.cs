using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNet.WebApi.SelfHost.Extensions
{
	internal static class HttpRequestExtensions
	{
		public static async Task<string> ExtractRequestBodyAsync(this HttpRequestMessage request, IApmLogger logger)
		{
			try
			{
				return await request.Content.ReadAsStringAsync();
			}
			catch (IOException ioException)
			{
				logger.Error()?.LogException(ioException, "IO Error reading request body");
			}
			catch (Exception e)
			{
				logger.Error()?.LogException(e, "Error reading request body");
			}

			return null;
		}
	}
}
