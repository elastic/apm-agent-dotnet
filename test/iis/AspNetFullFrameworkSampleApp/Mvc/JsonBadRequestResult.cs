using System.Web.Mvc;

namespace AspNetFullFrameworkSampleApp.Mvc
{
	public class JsonBadRequestResult : JsonResult
	{
		public override void ExecuteResult(ControllerContext context)
		{
			context.RequestContext.HttpContext.Response.StatusCode = 400;
			base.ExecuteResult(context);
		}
	}
}
