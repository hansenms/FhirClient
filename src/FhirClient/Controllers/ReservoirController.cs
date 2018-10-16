using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using FhirClient.Models;
using FhirClient.Services;
using System.Net.Http;
using System.Net;

namespace FhirClient.Controllers
{
    public class ReservoirController : Controller
    {
        private IPatientReservoir PatientReservoir { get; set; }
        public ReservoirController(IPatientReservoir reservoir)
        {
            PatientReservoir = reservoir;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["NumberOfPatients"] = await PatientReservoir.GetNumberOfPatientsAsync();
            //ViewData["NextPatient"] = await PatientReservoir.GetNextPatientJson();
            ViewData["NextPatient"] = Request.Headers["X-ZUMO-AUTH"];
            return View();
        }

        public async Task<string> Refresh()
        {
            string prefix = Request.IsHttps? "https" : "http";
            var baseAddress = new Uri($"{prefix}://{Request.Host.ToString()}");
            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                cookieContainer.Add(baseAddress, new Cookie("AppServiceAuthSession", Request.Cookies["AppServiceAuthSession"]));
                var result = await client.GetAsync("/.auth/refresh");
                result.EnsureSuccessStatusCode();
                return await result.Content.ReadAsStringAsync();
            }
        }
    }
}