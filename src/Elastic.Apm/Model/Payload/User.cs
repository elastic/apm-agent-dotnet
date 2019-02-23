using Elastic.Apm.Api;

namespace Elastic.Apm.Model.Payload
{
	internal class User : IUser
	{
		public string Id { get; set; }
		public string Email { get; set; }
		public string UserName { get; set; }
	}
}
