using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Newtonsoft.Json;

namespace Elastic.Apm.Model.Payload
{
	internal class Error : IError
	{
		public List<IErrorDetail> Errors { get; set; }
		public Service Service { get; set; }

		public class ErrorDetail : IErrorDetail
		{
			public ErrorDetail()
			{
				Timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
				Id = Guid.NewGuid();
			}

			public Context Context { get; set; }
			public string Culprit { get; set; }
			public ICapturedException Exception { get; set; }
			public Guid Id { get; }
			public string Timestamp { get; }
			public TransactionReference Transaction { get; set; }

			public class TransactionReference
			{
				public Guid Id { get; set; }
			}
		}
	}

	internal class CapturedException : ICapturedException
	{
		internal string Code { get; set; } //TODO

		public bool Handled { get; set; }

		/// <summary>
		/// The exception message, see: <see cref="Exception.Message" />
		/// </summary>
		public string Message { get; set; }

		public string Module { get; set; }

		[JsonProperty("Stacktrace")]
		public List<Stacktrace> StacktTrace { get; set; }

		/// <summary>
		/// The type of the exception class
		/// </summary>
		public string Type { get; set; }
	}
}
