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
using System.Net.Http;

namespace FhirClient.Controllers
{
    public class ResourceController : Controller
    {
        private IEasyAuthProxy _easyAuthProxy { get; set; }
        private IConfiguration Configuration { get; set; }
        public ResourceController(IEasyAuthProxy easyAuthProxy, IConfiguration config)
        {
            _easyAuthProxy = easyAuthProxy;
            Configuration = config;
        }

        [HttpGet("/Resource/{resourceType}/{resourceId}")]
        public async Task<IActionResult> GetAction(string resourceType, string resourceId)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Configuration["FhirServerUrl"]);
                var token = await _easyAuthProxy.GetAadAccessToken();

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                HttpResponseMessage result = await client.GetAsync($"/{resourceType}/{resourceId}");
                result.EnsureSuccessStatusCode();

                ViewData["ResourceJson"] = await result.Content.ReadAsStringAsync();
            }
            return View("Index");
        }
    }
}
