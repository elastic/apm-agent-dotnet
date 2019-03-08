using System;
using System.Collections.Generic;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Api
{
	public interface IError
	{
		 CapturedException CapturedException { get; set; }
	}
}
