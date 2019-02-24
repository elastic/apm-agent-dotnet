using System;
using System.Collections.Generic;

namespace Elastic.Apm.Model.Payload
{
	internal class Context
	{
		private readonly Lazy<Dictionary<string, string>> _tags = new Lazy<Dictionary<string, string>>();
		private readonly Lazy<User> _user = new Lazy<User>();
		private readonly Lazy<Response> _response = new Lazy<Response>();
		private readonly Lazy<Request> _request = new Lazy<Request>();

		public User User => _user.Value;
		public Request Request => _request.Value;
		public Response Response => _response.Value;

		public Dictionary<string, string> Tags => _tags.Value;
	}
}
