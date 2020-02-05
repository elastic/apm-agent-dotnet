using System.Web.Services;

namespace AspNetFullFrameworkSampleApp.Asmx
{
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	public class Health : WebService
	{
		[WebMethod]
		public string Ping()
		{
			return "Ok";
		}
	}
}
