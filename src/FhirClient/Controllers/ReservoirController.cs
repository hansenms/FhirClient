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
using Newtonsoft.Json.Linq;
using System.Text;

namespace FhirClient.Controllers
{
    public class ReservoirController : Controller
    {
        private IPatientReservoir PatientReservoir { get; set; }
        private IEasyAuthProxy _easyAuthProxy { get; set; }
        private IConfiguration Configuration { get; set; }

        public ReservoirController(IPatientReservoir reservoir, IEasyAuthProxy easyAuthProxy, IConfiguration config)
        {
            PatientReservoir = reservoir;
            _easyAuthProxy = easyAuthProxy;
            Configuration = config;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["NumberOfPatients"] = await PatientReservoir.GetNumberOfPatientsAsync();

            List<string> patients = await PatientReservoir.GetPatients(10);
            ViewData["PatientsInList"] = patients.Count; 
            return View(patients);
        }

        [HttpGet("/Reservoir/PushBundle/{bundleFileName}")]
        public async Task<IActionResult> PushBundle(string bundleFileName)
        {
            Console.WriteLine($"Bundle: {bundleFileName}");
            string bundleJson = await PatientReservoir.GetFhirBundleAndDelete(bundleFileName);
            if (!String.IsNullOrEmpty(bundleJson))
            {
                JObject o = JObject.Parse(bundleJson);

                JArray entries = (JArray)o["entry"];

                Console.WriteLine("Number of entries: " + entries.Count);

                for (int i = 0; i < entries.Count; i++)
                {
                    string entry_json = (((JObject)entries[i])["resource"]).ToString();
                    string resource_type = (string)(((JObject)entries[i])["resource"]["resourceType"]);
                    string id = (string)(((JObject)entries[i])["resource"]["id"]);

                    //Rewrite subject reference
                    if (((JObject)entries[i])["resource"]["subject"] != null)
                    {
                        string subject_reference = (string)(((JObject)entries[i])["resource"]["subject"]["reference"]);
                        if (!String.IsNullOrEmpty(subject_reference))
                        {
                            for (int j = 0; j < entries.Count; j++)
                            {
                                if ((string)(((JObject)entries[j])["fullUrl"]) == subject_reference)
                                {
                                    subject_reference = (string)(((JObject)entries[j])["resource"]["resourceType"]) + "/" + (string)(((JObject)entries[j])["resource"]["id"]);
                                    break;
                                }
                            }
                        }
                        ((JObject)entries[i])["resource"]["subject"]["reference"] = subject_reference;
                        entry_json = (((JObject)entries[i])["resource"]).ToString();
                    }

                    if (((JObject)entries[i])["resource"]["context"] != null)
                    {
                        string context_reference = (string)(((JObject)entries[i])["resource"]["context"]["reference"]);
                        if (!String.IsNullOrEmpty(context_reference))
                        {
                            for (int j = 0; j < entries.Count; j++)
                            {
                                if ((string)(((JObject)entries[j])["fullUrl"]) == context_reference)
                                {
                                    context_reference = (string)(((JObject)entries[j])["resource"]["resourceType"]) + "/" + (string)(((JObject)entries[j])["resource"]["id"]);
                                    break;
                                }
                            }
                        }
                        ((JObject)entries[i])["resource"]["context"]["reference"] = context_reference;
                        entry_json = (((JObject)entries[i])["resource"]).ToString();
                    }

                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = new Uri(Configuration["FhirServerUrl"]);
                        var token = await _easyAuthProxy.GetAadAccessToken();

                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                        client.DefaultRequestHeaders.Add("x-ms-consistency-level", "Eventual");
                        
                        StringContent content = new StringContent(entry_json, Encoding.UTF8, "application/json");

                        HttpResponseMessage uploadResult = null;

                        if (String.IsNullOrEmpty(id))
                        {
                            uploadResult = await client.PostAsync($"/{resource_type}", content);
                        }
                        else
                        {
                            uploadResult = await client.PutAsync($"/{resource_type}/{id}", content);
                        }

                        if (!uploadResult.IsSuccessStatusCode)
                        {
                            string resultContent = await uploadResult.Content.ReadAsStringAsync();
                            Console.WriteLine(resultContent);
                        }
                    }
                }
            }
            return Redirect("/Reservoir");
        }
    }
}