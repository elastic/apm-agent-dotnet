using System.Collections.Generic;

namespace Elastic.Apm.Api
{
	public interface ITransaction : IExecutionSegment
	{
		/// <summary>
		/// Any arbitrary contextual information regarding the event, captured by the agent, optionally provided by the user.
		/// This field is lazily initialized, you don't have to assign a value to it and you don't have to null check it either.
		/// </summary>
		Context Context { get; }

		/// <summary>
		/// An arbitrary mapping of additional metadata to store with the event.
		/// </summary>
		Dictionary<string, string> Custom { get; }

		/// <summary>
		/// A string describing the result of the transaction.
		/// This is typically the HTTP status code, or e.g. "success" for a background task.
		/// </summary>
		/// <value>The result.</value>
		string Result { get; set; }

		/// <summary>
		/// The type of the transaction.
		/// Example: 'request'
		/// </summary>
		string Type { get; set; }
	}
}
