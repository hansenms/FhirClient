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

        public IActionResult Index()
        {
            var client = GetClient();
            Bundle result = client.Search<Patient>();
            List<Patient> patientResults = new List<Patient>();
            foreach (var e in result.Entry)
            {
                patientResults.Add((Patient)e.Resource);
            }

            return View(patientResults);
        }


        private Hl7.Fhir.Rest.FhirClient GetClient()
        {
            var client = new Hl7.Fhir.Rest.FhirClient(Configuration["FhirServerUrl"]);
            client.OnBeforeRequest += (object sender, BeforeRequestEventArgs e) =>
            {
                e.RawRequest.Headers.Add("Authorization", $"Bearer {_easyAuthProxy.Headers["X-MS-TOKEN-AAD-ACCESS-TOKEN"]}");
            };
            client.PreferredFormat = ResourceFormat.Json;
            return client;
        }
    }
}