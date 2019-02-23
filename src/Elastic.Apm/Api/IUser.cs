namespace Elastic.Apm.Api
{
	public interface IUser
	{
		string Id { get; set; }
		string Email { get; set; }
		string UserName { get; set; }
	}
}
