using System;
using System.Collections.Generic;
using Elastic.Apm.Model;

namespace Elastic.Apm.Api
{
	public interface IError
	{
		string Id { get; }
	}
}
