using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyPaymentHelper.Helper
{
    public class DDHelper
    {
        private readonly ILogger<DDHelper> logger;
        private HttpClient _httpClient;

        public DDHelper(
            ILogger<DDHelper> logger)
        {
            this.logger = logger;

            WebProxy proxy = new WebProxy("http://127.0.0.1:10088");
            HttpClientHandler handler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true
            };
            this._httpClient = new HttpClient(handler);

            this._httpClient.Timeout = TimeSpan.FromSeconds(20);
        }

        public async Task<string> GetSessionAsync(string jq, string sessionID)
        {
            var result = await this._httpClient.PostJson($"https://dd.{jq}/OutProcessData", JsonConvert.SerializeObject(new
            {
                token = sessionID
            }));

            return JObject.Parse(result.Item2).SelectToken("data").ToString();
        }

        public async Task<bool> UpdateSessionAsync(string jq, string sessionData)
        {
            var result = await this._httpClient.PostJson($"https://dd.{jq}/UpdateProcess", sessionData);

            return JObject.Parse(result.Item2).SelectToken("status").ToObject<int>() == 1;
        }
    }
}
