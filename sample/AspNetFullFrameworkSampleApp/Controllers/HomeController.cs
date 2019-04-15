using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Elastic.Apm.Api;

namespace AspNetFullFrameworkSampleApp.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            return await Elastic.Apm.Agent.Tracer.CaptureTransaction("/Home/Index", ApiConstants.TypeRequest, async () =>
            {
                HttpClient httpClient = new HttpClient();
                await httpClient.GetAsync("https://elastic.co");
                return View();
            });
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
