using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
 
namespace FhirClient.Services
{
 
    public class AuthClaims {
        public string typ { get; set; }
        public string val { get; set; }
    }
 
    public class AuthMe {
        public string access_token { get; set; }
        public string id_token { get; set; }
        public string expires_on { get; set; }
        public string refresh_token { get; set; }
        public string user_id { get; set; }
        public string provider_name { get; set; }
        List<AuthClaims> user_claims { get; set; }
    }
 
    public interface IEasyAuthProxy
    {
        Microsoft.AspNetCore.Http.IHeaderDictionary Headers {get; }
        Task<string> GetAadAccessToken();
    }
 
    public class EasyAuthProxy: IEasyAuthProxy
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IHostingEnvironment _appEnvironment;
        private IHeaderDictionary _privateHeaders = null;
 
        public EasyAuthProxy(IHttpContextAccessor contextAccessor,
                             IHostingEnvironment appEnvironment)
        {
            _contextAccessor = contextAccessor;
            _appEnvironment = appEnvironment;
 
            string authMeFile = _appEnvironment.ContentRootPath + "/wwwroot/.auth/me"; 
            if (File.Exists(authMeFile)) {
                    try {
                    _privateHeaders = new HeaderDictionary();
                    List<AuthMe> authme = JsonConvert.DeserializeObject<List<AuthMe>>(File.ReadAllText(authMeFile));
 
                    _privateHeaders["X-MS-TOKEN-" + authme[0].provider_name.ToUpper() + "-ID-TOKEN"] = authme[0].id_token;
                    _privateHeaders["X-MS-TOKEN-" + authme[0].provider_name.ToUpper() + "-ACCESS-TOKEN"] = authme[0].access_token;
                    _privateHeaders["X-MS-TOKEN-" + authme[0].provider_name.ToUpper() + "EXPIRES-ON"] = authme[0].expires_on;
                    _privateHeaders["X-MS-CLIENT-PRINCIPAL-NAME"] = authme[0].user_id;
                } catch {
                    _privateHeaders = null;
                }
            }
        }
 
        public IHeaderDictionary Headers {
            get { 
                return _privateHeaders == null ? _contextAccessor.HttpContext.Request.Headers : _privateHeaders; 
            } 
        }


        public bool TokenIsExpired()
        {
            DateTime expires_on = DateTime.Parse(Headers["X-MS-TOKEN-AAD-EXPIRES-ON"]);
            return (expires_on - DateTime.Now).TotalMinutes < 1;
        }
        public async Task<string> GetAadAccessToken()
        {
            if (_privateHeaders == null && TokenIsExpired())
            {
                Console.WriteLine("Token is expired refreshing...");
                HttpRequest req = _contextAccessor.HttpContext.Request;
                string prefix = req.IsHttps? "https" : "http";
                var baseAddress = new Uri($"{prefix}://{req.Host.ToString()}");
                var cookieContainer = new CookieContainer();
                using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
                using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
                {
                    cookieContainer.Add(baseAddress, new Cookie("AppServiceAuthSession", req.Cookies["AppServiceAuthSession"]));
                    var result = await client.GetAsync("/.auth/refresh");
                    result.EnsureSuccessStatusCode();
                    result = await client.GetAsync("/.auth/me");
                    result.EnsureSuccessStatusCode();
                    List<AuthMe> authme = JsonConvert.DeserializeObject<List<AuthMe>>(await result.Content.ReadAsStringAsync());
                    return authme[0].access_token;
                }
            }
            else
            {
                return Headers["X-MS-TOKEN-AAD-ACCESS-TOKEN"];
            }
        }
    }
}