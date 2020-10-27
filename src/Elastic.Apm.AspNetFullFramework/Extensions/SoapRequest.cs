// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Specialized;
using System.IO;
using System.Xml;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNetFullFramework.Extensions
{
	internal static class SoapRequest
	{
		private const string SoapActionHeaderName = "SOAPAction";
		private const string ContentTypeHeaderName = "Content-Type";
		private const string SoapAction12ContentType = "application/soap+xml";

		/// <summary>
		/// Extracts the soap action from the header if exists only with Soap 1.1
		/// </summary>
		/// <param name="headers">The request headers</param>
		/// <param name="requestStream">The request stream</param>
		/// <param name="logger">The logger.</param>
		public static string ExtractSoapAction(NameValueCollection headers, Stream requestStream, IApmLogger logger)
		{
			try
			{
				return GetSoap11Action(headers) ?? GetSoap12Action(headers, requestStream);
			}
			catch (Exception e)
			{
				logger.Error()?.LogException(e, "Error reading soap action header");
			}

			return null;
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

		/// <summary>
		/// Lightweight parser that extracts the soap action from the xml body only with Soap 1.2
		/// </summary>
		/// <param name="headers">the request headers</param>
		/// <param name="requestStream">the request stream</param>
		private static string GetSoap12Action(NameValueCollection headers, Stream requestStream)
		{
			//[{"key":"Content-Type","value":"application/soap+xml; charset=utf-8"}]
			var contentType = headers.Get(ContentTypeHeaderName);
			if (contentType is null || !contentType.Contains(SoapAction12ContentType))
				return null;

			var stream = requestStream;
			if (!stream.CanSeek)
				return null;

			try
			{
				var action = GetSoap12ActionInternal(stream);
				return action;
			}
			finally
			{
				stream.Seek(0, SeekOrigin.Begin);
			}
		}

		internal static string GetSoap12ActionInternal(Stream stream)
		{
			try
			{
				var settings = new XmlReaderSettings
				{
					IgnoreProcessingInstructions = true,
					IgnoreComments = true,
					IgnoreWhitespace = true
				};

				using var reader = XmlReader.Create(stream, settings);
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

				return null;
			}
			catch (XmlException)
			{
				//previous code will skip some errors, but some others can raise an exception
				//for instance undeclared namespaces, typographical quotes, etc...
				//If that's the case we don't need to care about them here. They will flow somewhere else.
				return null;
			}
		}
	}
}
