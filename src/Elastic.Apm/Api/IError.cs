using System;
using System.Collections.Generic;

namespace Elastic.Apm.Api
{
	public interface IError
	{
		List<IErrorDetail> Errors { get; set; }
	}

	public interface IErrorDetail
	{
		string Culprit { get; set; }
		ICapturedException Exception { get; set; }
		Guid Id { get; }
	}

	public interface ICapturedException
	{
		/// <summary>
		/// The exception message, see: <see cref="Exception.Message" />
		/// </summary>
		string Message { get; set; }

		/// <summary>
		/// The type of the exception class
		/// </summary>
		string Type { get; set; }
	}
}
