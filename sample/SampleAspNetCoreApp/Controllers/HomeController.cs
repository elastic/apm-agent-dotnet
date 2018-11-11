using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SampleAspNetCoreApp.Data;
using SampleAspNetCoreApp.Models;

namespace SampleAspNetCoreApp.Controllers
{
    public class HomeController : Controller
    {
        SampleDataContext _sampleDataContext;

        public HomeController(SampleDataContext sampleDataContext)
        {
            _sampleDataContext = sampleDataContext;
        }

        public async Task<IActionResult> Index()
        {
            //TODO: Show this on the real UI
            foreach (var item in _sampleDataContext.Users)
            {
                Console.WriteLine(item.Name);
            }

            try
            {
                //TODO: turn this into a more realistic sample
                var httpClient = new HttpClient();
                var responseMsg = await httpClient.GetAsync("https://elastic.co");
                var responseStr = await responseMsg.Content.ReadAsStringAsync();
                Console.WriteLine(responseStr.Length);
            }
            catch
            {
                Console.WriteLine("Failed HTTP GET elastic.co");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddNewUser(string enteredName)
        {
            _sampleDataContext.Users.Add(
                new User
                {
                    Name = "TestName"
                });

            //TODO: get the real data
            await _sampleDataContext.SaveChangesAsync();

            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AddNewUser()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
