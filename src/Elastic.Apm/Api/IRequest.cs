using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Api
{
	public interface IRequest
	{
		string HttpVersion { get; set; }
		string Method { get; set; }
		string Body { get; set; }
	}
}
