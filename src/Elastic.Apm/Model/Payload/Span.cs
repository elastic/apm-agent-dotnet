using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elastic.Apm.Model.Payload
{
	public class Span : ISpan
	{
		public const string ActionExec = "exec";
		public const string ActionQuery = "query";

		public const string SubtypeHttp = "http";
		public const string SubtypeMssql = "mssql";
		public const string SubtypeSqLite = "sqlite";

		public const string TypeDb = "db";
		public const string TypeExternal = "external";

		private readonly DateTimeOffset _start;

		public Span(string name, string type, Transaction transaction)
		{
			Transaction = transaction;
			_start = DateTimeOffset.UtcNow;
			Start = (decimal)(_start - transaction.Start).TotalMilliseconds;
			Name = name;
			Type = type;

			var rnd = new Random();
			Id = rnd.Next();
		}

		public string Action { get; set; }

		public IContext Context { get; set; }

		/// <inheritdoc />
		/// <summary>
		/// The duration of the span.
		/// If it's not set (HasValue returns false) then the value
		/// is automatically calculated when <see cref="End" /> is called.
		/// </summary>
		/// <value>The duration.</value>
		public double? Duration { get; set; }

		public int Id { get; set; }

		public string Name { get; set; }

		[JsonProperty("Stacktrace")]
		public List<Stacktrace> StackTrace { get; set; }

		public decimal Start { get; set; }

		public string Subtype { get; set; }
		internal Transaction Transaction;

		public Guid TransactionId => Transaction.Id;

		public string Type { get; set; }

		public void End()
		{
			if (!Duration.HasValue) Duration = (DateTimeOffset.UtcNow - _start).TotalMilliseconds;

			Transaction?.SpansInternal.Add(this);
		}

		public void CaptureException(Exception exception, string culprit = null)
			=> Transaction?.CaptureException(exception, culprit);

		public void CaptureError(string message, string culprit, StackFrame[] frames)
			=> Transaction?.CaptureError(message, culprit, frames);

		public class ContextC : IContext
		{
			public IDb Db { get; set; }
			public IHttp Http { get; set; }
		}
	}

	public class Db : IDb
	{
		public string Instance { get; set; }
		public string Statement { get; set; }
		public string Type { get; set; }
	}

	public class Http : IHttp
	{
		public string Method { get; set; }
		public int StatusCode { get; set; }
		public string Url { get; set; }
	}
}
