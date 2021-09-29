// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Buffers;
using System.IO;
using System.Text;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using static Elastic.Apm.AspNetCore.Consts;

namespace Elastic.Apm.AspNetCore.Extensions
{
	internal static class HttpRequestExtensions
	{
		/// <summary>
		/// Extracts the request body, up to a specified maximum length.
		/// The request body that is read is buffered.
		/// </summary>
		/// <param name="request">The request</param>
		/// <param name="logger">The logger</param>
		/// <param name="configuration">The configuration snapshot</param>
		/// <returns></returns>
		public static string ExtractRequestBody(this HttpRequest request, IApmLogger logger, IConfiguration configuration)
		{
			string body = null;
			var longerThanMaxLength = false;

			try
			{
				//Ensure the request Microsoft.AspNetCore.Http.HttpRequest.Body can be read multiple times
				request.EnableBuffering();

				if (request.HasFormContentType)
				{
					var form = request.Form;

					var itemProcessed = 0;
					if (form != null && form.Count > 0)
					{
						var sb = new StringBuilder();
						foreach (var item in form)
						{
							sb.Append(item.Key);
							sb.Append("=");

							if (WildcardMatcher.IsAnyMatch(configuration.SanitizeFieldNames, item.Key))
								sb.Append(Elastic.Apm.Consts.Redacted);
							else
								sb.Append(item.Value);

							itemProcessed++;
							if (itemProcessed != form.Count)
								sb.Append("&");

							// perf: check length once per iteration and truncate at the end, rather than each append
							if (sb.Length > RequestBodyMaxLength)
							{
								longerThanMaxLength = true;
								break;
							}
						}

						body = sb.ToString(0, Math.Min(sb.Length, RequestBodyMaxLength));
					}
				}
				else
				{
					// allow synchronous reading of the request stream, which is false by default from 3.0 onwards.
					// Reading must be synchronous as it can happen within a synchronous diagnostic listener method
					var bodyControlFeature = request.HttpContext.Features.Get<IHttpBodyControlFeature>();
					if (bodyControlFeature != null)
						bodyControlFeature.AllowSynchronousIO = true;

					var requestBody = request.Body;
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
				}
			}
			catch (IOException ioException)
			{
				logger.Error()?.LogException(ioException, "IO Error reading request body");
			}
			catch (Exception e)
			{
				logger.Error()?.LogException(e, "Error reading request body");
			}

			if (longerThanMaxLength)
				logger.Debug()?.Log("truncated body to max length {MaxLength}", RequestBodyMaxLength);

			return body;
		}
	}
}
