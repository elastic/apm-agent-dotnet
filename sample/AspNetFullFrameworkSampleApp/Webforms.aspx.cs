using System;
using System.Web.UI;
using System.Web.Mvc;
using AspNetFullFrameworkSampleApp.Controllers;

namespace AspNetFullFrameworkSampleApp
{
	public partial class Webforms : Page
	{
		internal const string RoutedWebforms = nameof(RoutedWebforms);

		protected void Page_Load(object sender, EventArgs e)
		{
			Title = RouteData.RouteHandler == null
				? nameof(Webforms)
				: RoutedWebforms;

			// create a Html helper to use to render links to MVC actions
			var controllerContext = new ControllerContext(Request.RequestContext, new HomeController());
			Html = new HtmlHelper(
				new ViewContext(
					controllerContext,
					new WebFormView(controllerContext, "~/" + nameof(Webforms) + ".aspx"),
					new ViewDataDictionary(),
					new TempDataDictionary(),
					Response.Output
				),
				new PageViewDataContainer());
		}

		public class PageViewDataContainer : IViewDataContainer
		{
			public ViewDataDictionary ViewData { get; set; }
		}

		public HtmlHelper Html { get; private set; }
	}
}

