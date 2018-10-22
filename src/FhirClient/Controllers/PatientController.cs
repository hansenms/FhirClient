using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using FhirClient.Models;
using FhirClient.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirClient.Controllers
{
    public class PatientController : Controller
    {
        private IEasyAuthProxy _easyAuthProxy { get; set; }
        private IConfiguration Configuration { get; set; }

        public PatientController(IEasyAuthProxy easyAuthProxy, IConfiguration config)
        {
            _easyAuthProxy = easyAuthProxy;
            Configuration = config;
        }

        public async Task<IActionResult> Index()
        {
            var client = await GetClientAsync();
            Bundle result = null;
            
            if (!String.IsNullOrEmpty(Request.Query["ct"]))
            {
                string cont = Request.Query["ct"];
                result = client.Search<Patient>(new string [] { $"ct={cont}"});
            }
            else
            {
                result = client.Search<Patient>();
            }
            List<Patient> patientResults = new List<Patient>();
            if (result.Entry != null) {
                foreach (var e in result.Entry)
                {
                    patientResults.Add((Patient)e.Resource);
                }
            }

            if (result.NextLink != null) {
                ViewData["NextLink"] = result.NextLink.PathAndQuery;
            }

            return View(patientResults);
        }

        public async Task<string> CheckToken()
        {
            return await _easyAuthProxy.GetAadAccessToken();
        }

        private async Task<Hl7.Fhir.Rest.FhirClient> GetClientAsync()
        {
            var client = new Hl7.Fhir.Rest.FhirClient(Configuration["FhirServerUrl"]);
            var token = await _easyAuthProxy.GetAadAccessToken();
            client.OnBeforeRequest += (object sender, BeforeRequestEventArgs e) =>
            {
                e.RawRequest.Headers.Add("Authorization", $"Bearer {token}");
            };
            client.PreferredFormat = ResourceFormat.Json;
            return client;
        }
    }
}