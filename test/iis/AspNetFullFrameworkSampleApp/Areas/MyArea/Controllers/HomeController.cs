// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Web.Mvc;

namespace AspNetFullFrameworkSampleApp.Areas.MyArea.Controllers
{
	public class HomeController : Controller
	{
		internal const string HomePageRelativePath = "MyArea/Home";

		public ActionResult Index() => View();
	}
}
