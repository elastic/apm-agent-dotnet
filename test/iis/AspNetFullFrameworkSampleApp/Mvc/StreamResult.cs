// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.Web.Mvc;

namespace AspNetFullFrameworkSampleApp.Mvc
{
	public class StreamResult : ActionResult
	{
		private const int BufferSize = 4096;
		private readonly Stream _stream;
		private readonly string _contentType;
		private readonly int _statusCode;

		public StreamResult(Stream stream, string contentType, int statusCode = 200)
		{
			_stream = stream;
			_contentType = contentType;
			_statusCode = statusCode;
		}

		public override void ExecuteResult(ControllerContext context)
		{
			var response = context.RequestContext.HttpContext.Response;
			response.StatusCode = _statusCode;
			response.ContentType = _contentType;
			var outputStream = response.OutputStream;
			using (_stream)
			{
				var buffer = new byte[BufferSize];
				while (true)
				{
					var count = _stream.Read(buffer, 0, BufferSize);
					if (count != 0)
						outputStream.Write(buffer, 0, count);
					else
						break;
				}
			}
		}
	}
}
