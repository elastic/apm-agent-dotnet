// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Web.Mvc;

namespace AspNetFullFrameworkSampleApp.ActionFilters
{
	/// <summary>
	/// Redirects an authenticated user from an action intended for an unauthenticated user.
	/// </summary>
	public sealed class RedirectIfAuthenticatedAttribute : ActionFilterAttribute
	{
		public RedirectIfAuthenticatedAttribute() => Order = 1;

		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			if (filterContext.HttpContext.Request.IsAuthenticated)
				filterContext.Result = new RedirectResult("~/");
		}
	}
}
