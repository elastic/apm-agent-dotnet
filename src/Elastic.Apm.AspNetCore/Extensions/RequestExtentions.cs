using System;
using System.IO;
using System.Text;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace Elastic.Apm.AspNetCore.Extensions
{
    public static class HttpRequestExtensions
    {
		public static string ExtractRequestBody(this HttpRequest request, IApmLogger logger)
		{
			var injectedRequestStream = new MemoryStream();
			string body = null;

			try
			{
				using (var reader = new StreamReader(request.Body,
				encoding: Encoding.UTF8,
				detectEncodingFromByteOrderMarks: false,
				bufferSize: 1024 * 2))
				{
					//reset reader in case the body was read before
					request.Body.Position = 0;

					body = reader.ReadToEnd();
					// Reset the request body stream position so the next middleware can read it
					request.Body.Position = 0;
					// Truncate the body to the first 2kb if it's longer
					if (body.Length > Consts.RequestBodyMaxLength)
					{
						body = body.Substring(0, Consts.RequestBodyMaxLength);
					}

					//Write the body into the body in case it will be read by other components
					//after this one
					var bytesToWrite = Encoding.UTF8.GetBytes(body);
					injectedRequestStream.Write(bytesToWrite, 0, bytesToWrite.Length);
					injectedRequestStream.Seek(0, SeekOrigin.Begin);
					request.Body = injectedRequestStream;

				}
			} catch (IOException ioException){
				logger.Error()?.Log("IO Error reading request body , Error msg : {exception} , stack trace : {stacktrace} " , ioException.Message , ioException.StackTrace);
			} catch (Exception e) {
				//TODO : remove this after havning finalized all cases and testing all scenarios
				logger.Error()?.Log("General Error reading request body , Error msg : {exception} , stack trace : {stacktrace} ", e.Message, e.StackTrace);
			}
			return body;
        }

		
	}
}
