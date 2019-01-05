using Elastic.Apm.Model.Payload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AspNetFullFrameworkSampleApp.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            return await Elastic.Apm.Agent.Api.CaptureTransaction("/Home/Index", Transaction.TYPE_REQUEST, async () =>
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