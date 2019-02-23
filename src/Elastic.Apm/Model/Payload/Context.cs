using System;
using System.Collections.Generic;

namespace Elastic.Apm.Model.Payload
{
	internal class Context
	{
		private readonly Lazy<Dictionary<string, string>> _tags = new Lazy<Dictionary<string, string>>();
		private Request _request;
		private Response _response;
		private User _user;

		public User User
		{
			get => _user ?? (_user = new User());
			set => _user = value;
		}
		public Request Request
		{
			get => _request ?? (_request = new Request());
			set => _request = value;
		}

		public Response Response
		{
			get => _response ?? (_response = new Response());
			set => _response = value;
		}

		public Dictionary<string, string> Tags => _tags.Value;
	}
}
