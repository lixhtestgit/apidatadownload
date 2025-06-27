using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebApplication1.Helper
{
    public class GoogleTranslateHelper
    {
        private HttpClient _httpClient;

        public GoogleTranslateHelper(HttpClient httpClient)
        {
            this._httpClient = httpClient;
        }

        public async Task<string> Translate(string originContent)
        {
            GoogleTranslateRequest googleTranslateRequest = new GoogleTranslateRequest
            {
                q = new List<string> { originContent }
            };

            string url = "https://translation.googleapis.com/language/translate/v2?key=AIzaSyDXQKXaQojmQ3IfP8ABtTjdkhkgq03t0sA";
            string jsonData = JsonConvert.SerializeObject(googleTranslateRequest);

            var postResult = await this._httpClient.PostJson(url, jsonData);

            GoogleTranslateResponse googleTranslateResponse = JsonHelper.ConvertStrToJson<GoogleTranslateResponse>(postResult.Item2);
            string tranText = googleTranslateResponse.data.translations.FirstOrDefault().translatedText ?? "";

            return tranText;
        }

        public class GoogleTranslateRequest
        {
            private string _source = "zh-CN";

            /// <summary>
            /// 搜索词
            /// </summary>
            public List<string> q { get; set; }

            /// <summary>
            /// 目标语言
            /// </summary>
            public string target { get; set; } = "en-US";

            /// <summary>
            /// 原始语言
            /// </summary>
            public string source
            {
                get => this._source;
                set => this._source = value;
            }
        }

        public class GoogleTranslateResponse
        {
            public GoogleTranslateResponse.Data data { get; set; }

            public class Data
            {
                public List<GoogleTranslateResponse.Translation> translations { get; set; }
            }

            public class Translation
            {
                /// <summary>
                /// 目标翻译内容
                /// </summary>
                public string translatedText { get; set; }

                public string detectedSourceLanguage { get; set; }
            }
        }
    }
}
