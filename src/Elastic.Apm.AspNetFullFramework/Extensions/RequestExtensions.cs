using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using Elastic.Apm.Logging;


namespace Elastic.Apm.AspNetFullFramework.Extensions
{
	internal static class HttpRequestExtensions
	{
		private const string SoapActionHeaderName = "SOAPAction";
		private const string ContentTypeHeaderName = "Content-Type";
		private const string SoapAction12ContentType = "application/soap+xml";

		/// <summary>
		/// Extracts the soap action from the header if exists only with Soap 1.1
		/// </summary>
		/// <param name="request">The request.</param>
		/// <param name="logger">The logger.</param>
		public static string ExtractSoapAction(this HttpRequest request, IApmLogger logger)
		{
			try
			{
				return
					GetSoap11Action(request)
					?? GetSoap12Action(request);
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
		/// <param name="request">The request.</param>
		/// <param name="logger">The logger.</param>
		private static string GetSoap11Action(HttpRequest request)
		{
			var soapActionWithNamespace = request.Headers.Get(SoapActionHeaderName);
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
		/// <param name="request">The request.</param>
		/// <param name="logger">The logger.</param>
		private static string GetSoap12Action(HttpRequest request)
		{
			//[{"key":"Content-Type","value":"application/soap+xml; charset=utf-8"}]
			var contentType = request.Headers.Get(ContentTypeHeaderName);
			if (contentType?.Contains(SoapAction12ContentType) != true)
				return null;
			if (!request.InputStream.CanSeek)
				return null;

			try
			{
				var stream = request.InputStream;
				var action = GetSoap12ActionInternal(stream);
				return action;
			}
			finally
			{
				request.InputStream.Seek(0, SeekOrigin.Begin);
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
					IgnoreWhitespace = true,
					ConformanceLevel = ConformanceLevel.Auto,
					ValidationType = ValidationType.None,
					DtdProcessing = DtdProcessing.Ignore,
					ValidationFlags= System.Xml.Schema.XmlSchemaValidationFlags.None
				};

				var reader = XmlReader.Create(stream, settings);

				reader.MoveToContent();
				if (reader.LocalName != "Envelope")
					return null;

				if (reader.Read() && reader.LocalName == "Header")
					reader.Skip();

				if (reader.LocalName == "Body")
					if (reader.Read())
						return reader.LocalName;

				return null;
			}
			catch (XmlException ex)
			{
				//previous code will skip some errors, but some others can raise an exception
				//for instance undeclared namespaces, typographical quotes, etc...
				//If that's the case we don't need to care about them here. They will flow somewere else.
				return null;
			}
		}
	}
}
