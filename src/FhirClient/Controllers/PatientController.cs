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
            List<Patient> patientResults = new List<Patient>();

            try {
                if (!String.IsNullOrEmpty(Request.Query["ct"]))
                {
                    string cont = Request.Query["ct"];
                    result = client.Search<Patient>(new string [] { $"ct={cont}"});
                }
                else
                {
                    result = client.Search<Patient>();
                }
                
                if (result.Entry != null) {
                    foreach (var e in result.Entry)
                    {
                        patientResults.Add((Patient)e.Resource);
                    }
                }

                if (result.NextLink != null) {
                    ViewData["NextLink"] = result.NextLink.PathAndQuery;
                }

            } 
            catch (Exception e)
            {
                ViewData["ErrorMessage"] = e.Message;
            } 

            return View(patientResults);
        }

        [HttpGet("/Patient/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var client = await GetClientAsync();
            PatientRecord patientRecord = new PatientRecord();

            try
            {
                var patientResult = client.Search<Patient>(new string [] { $"_id={id}"});
                if ((patientResult.Entry != null) && (patientResult.Entry.Count > 0))
                {
                    patientRecord.Patient = (Patient)(patientResult.Entry[0].Resource);
                }

                if (patientRecord.Patient != null)
                {
                    patientRecord.Observations = new List<Observation>();
                    var observationResult = client.Search<Observation>(new string [] { $"subject=Patient/{patientRecord.Patient.Id}"});

                    while (observationResult != null) {
                        foreach (var o in observationResult.Entry)
                        {
                            patientRecord.Observations.Add((Observation)o.Resource);
                        }
                        observationResult = client.Continue(observationResult);
                    }

                    patientRecord.Encounters = new List<Encounter>();
                    var encounterResult = client.Search<Encounter>(new string [] { $"subject=Patient/{patientRecord.Patient.Id}"});

                    while (encounterResult != null) {
                        foreach (var e in encounterResult.Entry)
                        {
                            patientRecord.Encounters.Add((Encounter)e.Resource);
                        }
                        encounterResult = client.Continue(encounterResult);
                    }

                }
            }
            catch (Exception e)
            {
                ViewData["ErrorMessage"] = e.Message;
            }

            return View(patientRecord);
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