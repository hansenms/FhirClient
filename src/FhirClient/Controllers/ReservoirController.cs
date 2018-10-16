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
            using (var client = new HttpClient())
            {
                string prefix = Request.IsHttps? "https" : "http";
                Console.WriteLine($"{prefix}://{Request.Host.ToString()}");
                client.BaseAddress = new Uri($"{prefix}://{Request.Host.ToString()}");
                var result = await client.GetAsync("/.auth/refresh");
                return await result.Content.ReadAsStringAsync();
            }
        }
    }
}