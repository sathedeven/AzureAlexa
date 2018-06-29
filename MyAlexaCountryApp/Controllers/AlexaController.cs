using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MyAlexaCountryApp.Controllers
{
    [Produces("application/json")]
    [Route("api/Alexa")]
    public class AlexaController : Controller
    {
        [HttpPost]
        public async Task<SkillResponse> Index([FromBody]SkillRequest request)
        {
            switch (request.Request.Type)
            {

                case "IntentRequest": return await HandleIntentRequest(request);

                case "LaunchRequest": return await HandleLaunchRequest(request);

                default: throw new NotSupportedException($"Request type not supported: {request.Request.Type}");

            }

        }

        private async Task<SkillResponse> HandleIntentRequest(SkillRequest request)
        {
            var intentRequest = request.Request as IntentRequest;
            var intentName = intentRequest.Intent.Name;
            var country = intentRequest.Intent.Slots["Country"]?.Value;

            var countryInfo =  await CountryInfo(country);

            var outputSpeech = new PlainTextOutputSpeech
            {
                Text = countryInfo
            };

            var response = ResponseBuilder.Tell(outputSpeech);
            response.Response.ShouldEndSession = false;

            return response;
        }

        private Task<SkillResponse> HandleLaunchRequest(SkillRequest request)
        {
            var outputSpeech = new PlainTextOutputSpeech
            {
                Text = "Welcome to Country Info"
            };

            var response = ResponseBuilder.Tell(outputSpeech);
            response.Response.ShouldEndSession = false;

            return Task.FromResult<SkillResponse>(response);
        }

        private async Task<string> CountryInfo(string country)
        {
            StringBuilder sb = new StringBuilder();
            bool notFound = true;

            var client = new HttpClient();
            var path = "rest/v2/name/" + country;
            List<CountryInfo> lstCountryInfo = new List<CountryInfo>();

            client.BaseAddress = new Uri("https://restcountries.eu/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await client.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();

                lstCountryInfo = JsonConvert.DeserializeObject<List<CountryInfo>>(jsonString);

                sb.Append($" Capital is {lstCountryInfo[0].Capital} \n");
                sb.Append($" Region is {lstCountryInfo[0].Region} \n");
                sb.Append($" Sub Region is {lstCountryInfo[0].SubRegion} \n");
                sb.Append($" Currency is {lstCountryInfo[0].Currencies[0].Name } ( { lstCountryInfo[0].Currencies[0].Code} )  \n");
            }
            else
            {
                sb.Append("There was some problem in fetching the data. Please try after sometime.");
            }
            notFound = false;

            if (notFound)
                sb.Append("Sorry I don't know that. \n");

            return sb.ToString();
        }
    }


    [Serializable]
    public class CountryInfo
    {
        public string Name { get; set; }
        public string Capital { get; set; }

        public string Region { get; set; }

        public string SubRegion { get; set; }

        public List<Currency> Currencies { get; set; }
    }

    [Serializable]
    public class Currency
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
    }
}