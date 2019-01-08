using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;

namespace Elastic.Apm.Model.Payload
{
	public class Span : ISpan
	{
		public const string ACTION_EXEC = "exec";

		public const string ACTION_QUERY = "query";

		public const string SUBTYPE_HTTP = "http";
		public const string SUBTYPE_MSSQL = "mssql";
		public const string SUBTYPE_SQLITE = "sqlite";
		public const string TYPE_DB = "db";
		public const string TYPE_EXTERNAL = "external";
		internal Transaction transaction;

		private readonly DateTimeOffset start;

		public Span(string name, string type, Transaction transaction)
		{
			this.transaction = transaction;
			start = DateTimeOffset.UtcNow;
			Start = (decimal)(start - transaction.start).TotalMilliseconds;
			Name = name;
			Type = type;

			var rnd = new Random();
			Id = rnd.Next();
		}

		public string Action { get; set; }

		public IContext Context { get; set; }

		/// <summary>
		/// The duration of the span.
		/// If it's not set (HasValue returns false) then the value
		/// is automatically calculated when <see cref="End" /> is called.
		/// </summary>
		/// <value>The duration.</value>
		public double? Duration { get; set; }

		public int Id { get; set; }

		public string Name { get; set; }

		public List<Stacktrace> Stacktrace { get; set; }

		public decimal Start { get; set; }

		public string Subtype { get; set; }

		public Guid Transaction_id => transaction.Id;

		public string Type { get; set; }

		public void CaptureError(string message, string culprit, StackFrame[] frames)
			=> transaction?.CaptureError(message, culprit, frames);

		public void CaptureException(Exception exception, string culprit = null)
			=> transaction?.CaptureException(exception, culprit);

		public void End()
		{
			if (!Duration.HasValue) Duration = (DateTimeOffset.UtcNow - start).TotalMilliseconds;

			transaction?.spans.Add(this);
		}

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
		public int Status_code { get; set; }
		public string Url { get; set; }
	}
}
