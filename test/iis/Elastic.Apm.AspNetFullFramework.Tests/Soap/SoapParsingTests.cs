using System.IO;
using System.Text;
using Elastic.Apm.AspNetFullFramework.Extensions;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.AspNetFullFramework.Tests.Soap
{

	public class SoapParsingTests
	{
		#region Samples
		// Example 1: SOAP message containing a SOAP header block and a SOAP body

		private const string Sample1 = @"<env:Envelope xmlns:env=""http://www.w3.org/2003/05/soap-envelope"">
				 <env:Header>
				  <n:alertcontrol xmlns:n=""http://example.org/alertcontrol"">
				   <n:priority>1</n:priority>
				   <n:expires>2001-06-22T14:00:00-05:00</n:expires>
				  </n:alertcontrol>
				 </env:Header>
				 <env:Body>
				  <m:alert xmlns:m=""http://example.org/alert"">
				   <m:msg>Pick up Mary at school at 2pm</m:msg>
				  </m:alert>
				 </env:Body>
				</env:Envelope>";

		// Example 2: SOAP message containing an empty SOAP header and a SOAP body
		private const string Sample2 = @"<?xml version=""1.0""?>
				<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"" xmlns:m=""http://www.example.org"">
				  <soap:Header>
				  </soap:Header>
				  <soap:Body>
					<m:GetStockPrice>
					  <m:StockName>T</m:StockName>
					</m:GetStockPrice>
				  </soap:Body>
				</soap:Envelope>";

		// Example 2: SOAP message containing an empty SOAP header and a SOAP body
		private const string SampleWithComments = @"<?xml version=""1.0""?>
				<!-- some interesting comment -->
				<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"" xmlns:m=""http://www.example.org"">
				 <!-- some interesting comment -->
				 <soap:Header>
				  </soap:Header>
				  <!-- some interesting comment -->
				  <soap:Body>
					<!-- some interesting comment -->
					<m:GetStockPrice>
					  <m:StockName>T</m:StockName>
					</m:GetStockPrice>
				  </soap:Body>
				</soap:Envelope>";


		// Example : SOAP message containing only a body
		private const string SoapSampleOnlyBody = @"<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"" xmlns:m=""http://www.example.org"">
				  <soap:Body>
					<m:GetStockPrice>
					  <m:StockName>T</m:StockName>
					</m:GetStockPrice>
				  </soap:Body>
				</soap:Envelope>";

		private const string NotSoap = @"<Header></Header><Body><content></content></Body>";
		private const string NotXml = @"<Header></Head></Body>";
		private const string NotXml2 = @"dsfsfsd"; //this doesn't make the parser to fail
		private const string FaultyXml = @"<Envelope value=“1”></Envelope>"; //this one makes the parser to explode
		private const string FaultyXml2 = @"<Envelope unknown:value=""1""></Envelope>"; //this one makes the parser to explode

		private const string PartialMessage = @"<Envelope><Header></Header><Body><GetStockPrice>"
											+ "<\\|\t ##<<\r<<>\n>>//rfslk" //garbage
;

		/// <summary>
		/// This message is secured using WS-Security
		/// In fact, from a SOAP perspective, WS-Security is something that protects the contents of the message publishing one only method "EncryptedData".
		/// Projects using WS-Security should log the actual methdod called deeper in the processing pipeline
		/// </summary>
		private const string SoapWithWsSecurity = @"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
			  <SOAP-ENV:Header xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"">
				<wsse:Security xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd""
							   xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd""
							   soap:mustUnderstand=""1"">
				  <wsse:BinarySecurityToken
					EncodingType=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary""
					ValueType=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3""
					wsu:Id=""X509-B1165B2A578AFFC7D613649595665924"">...
				  </wsse:BinarySecurityToken>
				  <wsu:Timestamp wsu:Id=""TS-1"">
					<wsu:Created>2013-04-03T03:26:06.549Z</wsu:Created>
					<wsu:Expires>2013-04-03T03:31:06.549Z</wsu:Expires>
				  </wsu:Timestamp>
				  <xenc:EncryptedKey xmlns:xenc=""http://www.w3.org/2001/04/xmlenc#"" Id=""EK-B1165B2A578AFFC7D613649595666705"">
					<xenc:EncryptionMethod Algorithm=""http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p""></xenc:EncryptionMethod>
					<ds:KeyInfo xmlns:ds=""http://www.w3.org/2000/09/xmldsig#"">
					  <wsse:SecurityTokenReference>
						<ds:X509Data>
						  <ds:X509IssuerSerial>
							<ds:X509IssuerName>CN=Bob,O=IBM,C=US</ds:X509IssuerName>
							<ds:X509SerialNumber>24054675667389</ds:X509SerialNumber>
						  </ds:X509IssuerSerial>
						</ds:X509Data>
					  </wsse:SecurityTokenReference>
					</ds:KeyInfo>
					<xenc:CipherData>
					  <xenc:CipherValue>...</xenc:CipherValue>
					</xenc:CipherData>
					<xenc:ReferenceList>
					  <xenc:DataReference URI=""#ED-4""></xenc:DataReference>
					  <xenc:DataReference URI=""#ED-5""></xenc:DataReference>
					  <xenc:DataReference URI=""#ED-6""></xenc:DataReference>
					</xenc:ReferenceList>
				  </xenc:EncryptedKey>
				  <xenc:EncryptedData
					xmlns:xenc=""http://www.w3.org/2001/04/xmlenc#"" Id=""ED-6"" Type=""http://www.w3.org/2001/04/xmlenc#Element"">
					<xenc:EncryptionMethod Algorithm=""http://www.w3.org/2001/04/xmlenc#aes128-cbc""></xenc:EncryptionMethod>
					<ds:KeyInfo xmlns:ds=""http://www.w3.org/2000/09/xmldsig#"">
					  <wsse:SecurityTokenReference
						xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd""
						xmlns:wsse11=""http://docs.oasis-open.org/wss/oasis-wss-wssecurity-secext-1.1.xsd""
						wsse11:TokenType=""http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#EncryptedKey"">
						<wsse:Reference URI=""#EK-B1165B2A578AFFC7D613649595666705""></wsse:Reference>
					  </wsse:SecurityTokenReference>
					</ds:KeyInfo>
					<xenc:CipherData>
					  <xenc:CipherValue>...</xenc:CipherValue>
					</xenc:CipherData>
				  </xenc:EncryptedData>
				  <xenc:EncryptedData xmlns:xenc=""http://www.w3.org/2001/04/xmlenc#""
									  Id=""ED-5""
									  Type=""http://www.w3.org/2001/04/xmlenc#Element"">
					<xenc:EncryptionMethod Algorithm=""http://www.w3.org/2001/04/xmlenc#aes128-cbc""></xenc:EncryptionMethod>
					<ds:KeyInfo xmlns:ds=""http://www.w3.org/2000/09/xmldsig#"">
					  <wsse:SecurityTokenReference
						xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd""
						xmlns:wsse11=""http://docs.oasis-open.org/wss/oasis-wss-wssecurity-secext-1.1.xsd""
						wsse11:TokenType=""http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#EncryptedKey"">
						<wsse:Reference URI=""#EK-B1165B2A578AFFC7D613649595666705""></wsse:Reference>
					  </wsse:SecurityTokenReference>
					</ds:KeyInfo>
					<xenc:CipherData>
					  <xenc:CipherValue>...</xenc:CipherValue>
					</xenc:CipherData>
				  </xenc:EncryptedData>
				</wsse:Security>
			  </SOAP-ENV:Header>
			  <soap:Body xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd""
						 wsu:Id=""Id-1788936596"">
				<xenc:EncryptedData xmlns:xenc=""http://www.w3.org/2001/04/xmlenc#""
									Id=""ED-4""
									Type=""http://www.w3.org/2001/04/xmlenc#Content"">
				  <xenc:EncryptionMethod Algorithm=""http://www.w3.org/2001/04/xmlenc#aes128-cbc""></xenc:EncryptionMethod>
				  <ds:KeyInfo xmlns:ds=""http://www.w3.org/2000/09/xmldsig#"">
					<wsse:SecurityTokenReference
					  xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd""
					  xmlns:wsse11=""http://docs.oasis-open.org/wss/oasis-wss-wssecurity-secext-1.1.xsd""
					  wsse11:TokenType=""http://docs.oasis-open.org/wss/oasis-wss-soap-message-security-1.1#EncryptedKey"">
					  <wsse:Reference URI=""#EK-B1165B2A578AFFC7D613649595666705""></wsse:Reference>
					</wsse:SecurityTokenReference>
				  </ds:KeyInfo>
				  <xenc:CipherData>
					<xenc:CipherValue>...</xenc:CipherValue>
				  </xenc:CipherData>
				</xenc:EncryptedData>
			  </soap:Body>
			</soap:Envelope>";
		#endregion

		[Theory]
		[InlineData(Sample1, "alert")]
		[InlineData(Sample2, "GetStockPrice")]
		[InlineData(SampleWithComments, "GetStockPrice")]
		[InlineData(SoapSampleOnlyBody, "GetStockPrice")]
		[InlineData(SoapWithWsSecurity, "EncryptedData")] //special
		[InlineData(PartialMessage, "GetStockPrice")]
		[InlineData(NotSoap, null)]
		[InlineData(NotXml, null)]
		[InlineData(NotXml2, null)]
		[InlineData(FaultyXml, null)]
		[InlineData(FaultyXml2, null)]
		public void Soap12Parser_ParsesHeaderAndBody(string soap, string expectedAction)
		{
			var requestStream = new MemoryStream(Encoding.UTF8.GetBytes(soap));
			var action = SoapRequest.GetSoap12ActionFromInputStream(new NoopLogger(), requestStream);

			action.Should().Be(expectedAction);
		}
	}
}
