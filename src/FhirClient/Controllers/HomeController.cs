using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FhirClient.Models;
using FhirClient.Services;

namespace FhirClient.Controllers
{
    public class HomeController : Controller
    {
        private IEasyAuthProxy _easyAuthProxy { get; set; }
        public HomeController(IEasyAuthProxy easyAuthProxy)
        {
            _easyAuthProxy = easyAuthProxy;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["token"] = _easyAuthProxy.Headers["X-MS-TOKEN-AAD-ACCESS-TOKEN"];
            ViewData["UPN"] = _easyAuthProxy.Headers["X-MS-CLIENT-PRINCIPAL-NAME"];
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
