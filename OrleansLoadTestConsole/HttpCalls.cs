using LoadTest.Grains.Interfaces.Models;
using LoadTest.SharedBase.Helpers;
using LoadTest.SharedBase.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrleansLoadTestConsole
{
    public class HttpCalls : ITestCalls, IDisposable
    {
        HttpClient _httpClient;

        public HttpCalls(string baseUrl)
        {
            var contentType = new MediaTypeWithQualityHeaderValue("application/json");
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Add(contentType);

        }

        public async Task WarmUp(int grainId)
        {
            throw new NotImplementedException();
        }

        public async Task Post(DataClass data)
        {
            var api = "/NumberStore";

            var contentData = new StringContent(data.HttpPayloadJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(api, contentData);

            if (response.IsSuccessStatusCode)
            {
                //Console.WriteLine($"Success: {data.GrainId}");
            }
            else
            {
                DisplayHelper.WriteLine($"FAIL: {data.GrainId}. {response.ReasonPhrase}");
            }

        }


        public async Task<NumberInfo> GetGrainData(int grainId)
        {
            var api = "/NumberStore?grainId=" + grainId;

            var response = await _httpClient.GetAsync(api);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Success: {grainId}");
                // Use Newtonsoft as System.Text.Json doesn't like the date format.
                var result = JsonConvert.DeserializeObject<NumberInfo>(responseBody);
                return result;
            }
            else
            {
                DisplayHelper.WriteLine($"FAIL: {grainId}. {response.ReasonPhrase}");
                return null;
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
