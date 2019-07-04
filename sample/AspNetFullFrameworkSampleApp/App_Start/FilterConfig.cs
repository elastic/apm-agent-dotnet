using System.Web.Mvc;

namespace AspNetFullFrameworkSampleApp
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters) =>
			filters.Add(new HandleErrorAttribute());
	}
}
