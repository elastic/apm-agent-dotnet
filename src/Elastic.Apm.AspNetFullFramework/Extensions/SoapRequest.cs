// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Buffers;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNetFullFramework.Extensions
{
	/// <summary>
	/// Extract details about a SOAP request from a HTTP request
	/// </summary>
	internal static class SoapRequest
	{
		private const string SoapActionHeaderName = "SOAPAction";
		private const string ContentTypeHeaderName = "Content-Type";
		private const string SoapAction12ContentType = "application/soap+xml";

		/// <summary>
		/// Try to extract a Soap 1.1 or Soap 1.2 action from the request.
		/// </summary>
		/// <param name="logger">The logger</param>
		/// <param name="request">The request</param>
		/// <param name="soapAction">The extracted soap action. <c>null</c> if no soap action is extracted</param>
		/// <returns><c>true</c> if a soap action can be extracted, <c>false</c> otherwise.</returns>
		public static bool TryExtractSoapAction(IApmLogger logger, HttpRequest request, out string soapAction)
		{
			soapAction = null;
			try
			{
				var headers = request.Unvalidated.Headers;
				soapAction = GetSoap11Action(headers);
				if (soapAction != null)
					return true;

				if (IsSoap12Action(headers))
				{
					soapAction = GetSoap12ActionFromHttpRequest(logger, request);
					if (soapAction != null)
						return true;
				}
			}
			catch (Exception e)
			{
				logger.Error()?.LogException(e, "Error extracting soap action");
			}
			return false;
		}

		/// <summary>
		/// Extracts the soap action from the header if exists only with Soap 1.1
		/// </summary>
		/// <param name="headers">the request headers</param>
		private static string GetSoap11Action(NameValueCollection headers)
		{
			var soapActionWithNamespace = headers.Get(SoapActionHeaderName);
			if (!string.IsNullOrWhiteSpace(soapActionWithNamespace))
			{
				var indexPosition = soapActionWithNamespace.LastIndexOf(@"/", StringComparison.InvariantCulture);
				if (indexPosition != -1) return soapActionWithNamespace.Substring(indexPosition + 1).TrimEnd('\"');
			}
			return null;
		}

		private static bool IsSoap12Action(NameValueCollection headers)
		{
			var contentType = headers.Get(ContentTypeHeaderName);
			return contentType != null && contentType.Contains(SoapAction12ContentType);
		}

		internal static string GetSoap12ActionFromHttpRequest(IApmLogger logger, HttpRequest request)
		{
			//
			// If the input stream has already been read bufferless, we can't inspect it
			//
			if (request.ReadEntityBodyMode == ReadEntityBodyMode.Bufferless)
			{
				logger.Debug()?.Log("Request.ReadEntityBodyMode is 'Bufferless'");
				return null;
			}
			//
			// SOAP 1.2 action must be read from body data.
			// Using the buffered input stream can have side effects with application code that
			// runs before or after this.
			// To be safe, we only access the stream directly if we have random access to it.
			//
			var stream = request.InputStream;
			if (!stream.CanSeek)
			{
				logger.Debug()?.Log("Request.InputStream is not seekable");
				return null;
			}

			return GetSoap12ActionFromInputStream(logger, stream);
		}

		internal static string GetSoap12ActionFromInputStream(IApmLogger logger, Stream stream)
		{ 
			var shared = ArrayPool<byte>.Shared;
			var bytes = shared.Rent((int)stream.Length);
			try
			{
				stream.Position = 0;
				stream.Read(bytes, 0, bytes.Length);
				stream.Position = 0;

				var settings = new XmlReaderSettings
				{
					IgnoreProcessingInstructions = true,
					IgnoreComments = true,
					IgnoreWhitespace = true
				};
				using var streamReader = new StreamReader(new MemoryStream(bytes));
				using var reader = XmlReader.Create(streamReader, settings);
				reader.MoveToContent();
				if (reader.LocalName != "Envelope")
					return null;

				if (reader.Read() && reader.LocalName == "Header")
					reader.Skip();

				if (reader.LocalName == "Body")
				{
					if (reader.Read())
						return reader.LocalName;
				}
			}
			catch (XmlException e)
			{
				// The previous code will skip some errors, but some others can raise an exception
				// for instance undeclared namespaces, typographical quotes, etc...
				// If that's the case we don't need to care about them here. They will flow somewhere else.
				logger.Trace()?.LogException(e, "Error while trying to read SOAP 1.2 action");
			}
			finally
			{
				shared.Return(bytes);
			}
			return null;
		}
	}
}
