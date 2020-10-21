using System;
using System.Web.UI;
using System.Web.Mvc;

namespace AspNetFullFrameworkSampleApp
{
	public partial class Webforms : Page
	{
		internal const string RoutedWebforms = nameof(RoutedWebforms);

		protected void Page_Load(object sender, EventArgs e) =>
			// Determine if the PageRouteHandler routed here, or whether
			// the page was accessed from the .aspx virtual path
			Title = RouteData.RouteHandler == null
				? nameof(Webforms)
				: RoutedWebforms;

	}
}

