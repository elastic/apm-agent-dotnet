// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace Elastic.Apm.Helpers
{
	internal static class RequestBodyStreamHelper
	{
		internal const int RequestBodyMaxLength = 10000;

		internal static string ToString(Stream requestBody, out bool longerThanMaxLength)
		{
			longerThanMaxLength = false;

			if (requestBody == null) return null;

			string body = null;
			requestBody.Position = 0;
			var arrayPool = ArrayPool<char>.Shared;
			var capacity = 512;
			var buffer = arrayPool.Rent(capacity);
			var totalRead = 0;
			int read;

			// requestBody.Length is 0 on initial buffering - length relates to how much has been read and buffered.
			// Read to just beyond request body max length so that we can determine if truncation will occur
			try
			{
				// TODO: can we assume utf-8 encoding?
				using var reader = new StreamReader(requestBody, Encoding.UTF8, false, buffer.Length, true);
				while ((read = reader.Read(buffer, 0, capacity)) != 0)
				{
					totalRead += read;
					if (totalRead > RequestBodyMaxLength)
					{
						longerThanMaxLength = true;
						break;
					}
				}
			}
			finally
			{
				arrayPool.Return(buffer);
			}

			requestBody.Position = 0;
			capacity = Math.Min(totalRead, RequestBodyMaxLength);
			buffer = arrayPool.Rent(capacity);

			try
			{
				using var reader = new StreamReader(requestBody, Encoding.UTF8, false, RequestBodyMaxLength, true);
				read = reader.ReadBlock(buffer, 0, capacity);
				body = new string(buffer, 0, read);
			}
			finally
			{
				arrayPool.Return(buffer);
			}

			requestBody.Position = 0;

			return body;
		}
	}
}
