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
            ViewData["NextPatient"] = await PatientReservoir.GetNextPatientJson();
            return View();
        }
    }
}