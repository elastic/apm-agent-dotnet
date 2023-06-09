// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Mvc;

namespace SampleAspNetCoreApp.Areas.MyArea.Controllers
{
	[Area("MyArea")]
	public class HomeController : Controller
	{
		// GET
		public IActionResult Index() => View();
	}
}
