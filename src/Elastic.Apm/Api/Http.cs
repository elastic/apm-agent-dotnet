// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Net.Http;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// An object containing contextual data of the related http request.
	/// It can be attached to an <see cref="ISpan" /> through <see cref="ISpan.Context" />
	/// </summary>
	public class Http
	{
		private Uri _originalUrl;
		private string _url;

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Method { get; set; }

		/// <summary>
		/// The Url in its original form as it was passed to the Agent, without sanitization or any other trimming.
		/// </summary>
		internal Uri OriginalUrl => _originalUrl;

		[JsonProperty("status_code")]
		public int StatusCode { get; set; }

		/// <summary>
		/// Sets the URL of the HTTP request.
		/// The setter will parse and sanitize the URL and filter out user name and password from the URL in case it contains
		/// those.
		/// In case you have an <see cref="Uri" /> instance, consider using the <see cref="SetUrl" /> method on this class.
		/// </summary>
		public string Url
		{
			get => _url;
			set => _url = Sanitize(value, out var newValue, out _originalUrl) ? newValue : value;
		}

		/// <summary>
		/// Sets the <see cref="Url" /> string property directly with a <see cref="Uri" /> instance.
		/// The advantage of using this method is that the sanitization of the
		/// <param name="uri"></param>
		/// is
		/// allocation free in case there is nothing to sanitize in the <paramref name="uri" />.
		/// </summary>
		/// <param name="uri"></param>
		internal void SetUrl(Uri uri)
		{
			_originalUrl = uri;
			try
			{
				_url = string.IsNullOrEmpty(uri.UserInfo) ? uri.ToString() : SanitizeUserNameAndPassword(uri).ToString();
			}
			catch
			{
				_url = string.Empty;
			}
		}

		/// <summary>
		/// Removes the username and password from the <paramref name="uri" /> and returns it as a <see cref="string" />.
		/// If there is no username and password in the <paramref name="uri" />, the simple string representation is returned.
		/// </summary>
		/// <param name="uri">The URI that you'd like to sanitize.</param>
		/// <returns>The string representation of <paramref name="uri" /> without username and password.</returns>
		internal static string Sanitize(Uri uri)
		{
			Sanitize(uri, out var result);
			return result;
		}

		/// <summary>
		/// Returns the .ToString representation of an <see cref="HttpRequestMessage" />
		/// but makes sure that the username and the password is sanitized.
		/// </summary>
		/// <param name="msg"></param>
		/// <returns></returns>
		internal static string HttpRequestSanitizedToString(HttpRequestMessage msg)
		{
			if (!string.IsNullOrEmpty(msg.RequestUri?.UserInfo))
				msg.RequestUri = new Uri(Sanitize(msg.RequestUri));

			return msg.ToString();
		}

		private static bool Sanitize(string uriString, out string result, out Uri originalUrl)
		{
			try
			{
				originalUrl = new Uri(uriString, UriKind.RelativeOrAbsolute);
				return Sanitize(originalUrl, out result);
			}
			catch
			{
				result = null;
				originalUrl = null;
				return false;
			}
		}

		/// <summary>
		/// Returns <code>true</code> if sanitization was applied, <code>false</code> otherwise.
		/// In some cases turning a string into a URL and then turning it back to a string adds a trailing `/`.
		/// To avoid this problem, in the <paramref name="result" /> parameter the input is returned if there is nothing to change
		/// on the input.
		/// </summary>
		/// <param name="uriString">The Uri to sanitize.</param>
		/// <param name="result">
		/// The result, which is the sanitized string. If no sanitization was needed
		/// (because there was no username& password in the URL) then this contains the <paramref name="result" /> parameter.
		/// </param>
		/// <returns></returns>
		internal static bool Sanitize(string uriString, out string result) =>
			Sanitize(uriString, out result, out _);

		internal static bool Sanitize(Uri uri, out string result)
		{
			try
			{
				if (string.IsNullOrEmpty(uri.UserInfo))
				{
					result = uri.ToString();
					return false;
				}
				result = SanitizeUserNameAndPassword(uri).ToString();
				return true;
			}
			catch
			{
				result = null;
				return false;
			}
		}

		private static Uri SanitizeUserNameAndPassword(Uri uri)
		{
			var builder = new UriBuilder(uri)
			{
				UserName = "[REDACTED]",
				Password = "[REDACTED]"
			};
			return builder.Uri;
		}
	}
}
