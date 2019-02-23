using Elastic.Apm.Api;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Model.Payload
{
	internal class User : IUser
	{
		private string _id;
		private string _userName;
		private string _email;

		public string Id
		{
			get => _id;
			set => _id = value.TrimToMaxLength();
		}


		public string Email
		{
			get => _email;
			set => _email = value.TrimToMaxLength();
		}


		public string UserName
		{
			get => _userName;
			set => _userName = value.TrimToMaxLength();
		}
	}
}
