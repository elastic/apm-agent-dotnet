// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Net.Http;
using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.Libraries.Newtonsoft.Json;

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

		[MaxLength]
		public string Method { get; set; }

		/// <summary>
		/// The Url in its original form as it was passed to the Agent, without sanitization or trimming.
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
			set => _url = Sanitization.TrySanitizeUrl(value, out var newValue, out _originalUrl) ? newValue : value;
		}

		/// <summary>
		/// Sets the <see cref="Url" /> string property directly with a <see cref="Uri" /> instance.
		/// The advantage of using this method is that the sanitization of the <paramref name="uri" />
		/// is allocation free in case there is nothing to sanitize in the <paramref name="uri" />.
		/// </summary>
		/// <param name="uri"></param>
		internal void SetUrl(Uri uri)
		{
			_originalUrl = uri;
			try
			{
				_url = uri.Sanitize().ToString();
			}
			catch
			{
				_url = string.Empty;
			}
		}
	}
}
