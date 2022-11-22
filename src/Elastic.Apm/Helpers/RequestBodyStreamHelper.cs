// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Elastic.Apm.Helpers
{
	internal struct ReadRequestBodyResult
	{
		internal ReadRequestBodyResult(string body, bool isLongerThanMaxLength)
		{
			Body = body;
			IsLongerThanMaxLength = isLongerThanMaxLength;
		}

		internal string Body { get; }
		internal bool IsLongerThanMaxLength { get; }
	}

	internal static class RequestBodyStreamHelper
	{
		internal const int RequestBodyMaxLength = 10000;

		internal static async Task<ReadRequestBodyResult> ToString(Stream requestBody)
		{
			var longerThanMaxLength = false;

			if (requestBody == null) return new ReadRequestBodyResult();

			string body;

			var capacity = 512;
			var totalRead = 0;
			var arrayPool = ArrayPool<char>.Shared;
			char[] buffer;

			if (requestBody.Length == 0)
			{
				buffer = arrayPool.Rent(capacity);

				// requestBody.Length is 0 on initial buffering - length relates to how much has been read and buffered.
				// Read to just beyond request body max length so that we can determine if truncation will occur
				try
				{
					// TODO: can we assume utf-8 encoding?
					using var reader = new StreamReader(requestBody, Encoding.UTF8, false, buffer.Length, true);
					int read;
					while ((read = await reader.ReadAsync(buffer, 0, capacity)) != 0)
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
			}
			else
			{
				totalRead = (int)requestBody.Length;
				longerThanMaxLength = totalRead > RequestBodyMaxLength;
			}

			capacity = Math.Min(totalRead, RequestBodyMaxLength);
			buffer = arrayPool.Rent(capacity);

			try
			{
				using var reader = new StreamReader(requestBody, Encoding.UTF8, false, RequestBodyMaxLength, true);
				var read = await reader.ReadBlockAsync(buffer, 0, capacity);
				body = new string(buffer, 0, read);
			}
			finally
			{
				arrayPool.Return(buffer);
			}

			requestBody.Position = 0;

			return new ReadRequestBodyResult(body, longerThanMaxLength);
		}
	}
}
