using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore.Extensions
{
	public static class HttpRequestExtensions
	{
		/// <summary>
		/// Extracts the request body using measure to prevent the 'read once' problem (cannot read after the body ha been already
		/// read).
		/// </summary>
		/// <param name="request"></param>
		/// <param name="logger"></param>
		/// <returns></returns>
		public static async Task<string> ExtractRequestBodyAsync(this HttpRequest request, IApmLogger logger)
		{
			string body = null;

			try
			{
				request.EnableBuffering();
				request.Body.Position = 0;

				using (var reader = new StreamReader(request.Body,
					Encoding.UTF8,
					false,
					1024 * 2,
					true))
					body = await reader.ReadToEndAsync();

				// Truncate the body to the first 2kb if it's longer
				if (body.Length > Consts.RequestBodyMaxLength) body = body.Substring(0, Consts.RequestBodyMaxLength);
				request.Body.Position = 0;
			}
			catch (IOException ioException)
			{
				logger.Error()?.LogException(ioException, "IO Error reading request body");
			}
			catch (Exception e)
			{
				logger.Error()?.LogException(e, "Error reading request body");
			}
			return body;
		}
	}
}
