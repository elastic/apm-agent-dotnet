// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Web.Mvc;

namespace AspNetFullFrameworkSampleApp.Areas.MyArea
{
	public class MyAreaRegistration : AreaRegistration
	{
		public override void RegisterArea(AreaRegistrationContext context) =>
			context.MapRoute("MyArea_Default",
				"MyArea/{controller}/{action}/{id}",
				new { action = "Index", id = UrlParameter.Optional },
				new[] { "AspNetFullFrameworkSampleApp.Areas.MyArea.Controllers" });

		public override string AreaName => "MyArea";
	}
}
