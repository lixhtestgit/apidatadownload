using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public partial class HttpClientExtension
    {
        public const string CONTENT_TYPE_URLENCODED = "application/x-www-form-urlencoded";

        public const string CONTENT_TYPE_JSON = "application/json";

        public const string CONTENT_TYPE_TEXT = "text/plain";

        public const string CONTENT_TYPE_XML = "application/xml";

        /// <summary>
        /// POST方式发送信息(x-www-form-urlencoded字典格式)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="postDict">x-www-form-urlencoded字典格式</param>
        /// <param name="headerDict">http头信息</param>
        /// <param name="contentType">内容格式类型，默认为：application/x-www-form-urlencoded</param>
        /// <returns></returns>
        public static async Task<(HttpStatusCode, string, Dictionary<string, List<string>>)> PostForm(this HttpClient client, string url, IDictionary<string, string> postDict, IDictionary<string, string> headerDict, string contentType = CONTENT_TYPE_URLENCODED)
        {
            string responseText = null;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            Dictionary<string, List<string>> responseHeaderDic = null;
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                if (headerDict?.Count > 0)
                {
                    foreach (KeyValuePair<string, string> pair in headerDict)
                    {
                        request.Headers.Add(pair.Key, pair.Value);
                    }
                }
                using (FormUrlEncodedContent content = new FormUrlEncodedContent(postDict ?? new Dictionary<string, string>(0)))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                    request.Content = content;

                    using (HttpResponseMessage responseMessage = await client.SendAsync(request))
                    {
                        statusCode = responseMessage.StatusCode;
                        responseHeaderDic = responseMessage.Headers.ToDictionary(m => m.Key, ele => ele.Value.ToList());
                        try
                        {
                            responseText = await responseMessage.Content.ReadAsStringAsync();
                        }
                        catch (Exception ex)
                        {
                            responseText = $"statusCode:{statusCode},ex:{ex.ToString()}";
                        }
                    }
                }
            }
            return (statusCode, responseText, responseHeaderDic);
        }

        /// <summary>
        /// POST方式发送信息(RAW格式)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="raw">字符串</param>
        /// <param name="headerDict">http头信息</param>
        /// <param name="contentType">内容格式类型，默认为：application/json</param>
        /// <returns></returns>
        public static async Task<(HttpStatusCode, string)> PostJson(this HttpClient client, string url, string raw)
        {
            var postResult = await client.PostNoForm(url, raw, null, CONTENT_TYPE_JSON);
            return (postResult.Item1, postResult.Item2);
        }

        /// <summary>
        /// POST方式发送信息(RAW格式)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="raw">字符串</param>
        /// <param name="headerDict">http头信息</param>
        /// <param name="contentType">内容格式类型，默认为：application/json</param>
        /// <returns></returns>
        public static async Task<(HttpStatusCode, string)> PostJson(this HttpClient client, string url, string raw, IDictionary<string, string> headerDict)
        {
            var postResult = await client.PostNoForm(url, raw, headerDict, CONTENT_TYPE_JSON);
            return (postResult.Item1, postResult.Item2);
        }

        /// <summary>
        /// POST方式发送信息(Xml格式)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="raw">字符串</param>
        /// <param name="headerDict">http头信息</param>
        /// <param name="contentType">内容格式类型，默认为：application/xml</param>
        /// <returns></returns>
        public static async Task<(HttpStatusCode, string)> PostXml(this HttpClient client, string url, string raw)
        {
            var postResult = await client.PostNoForm(url, raw, null, CONTENT_TYPE_XML);
            return (postResult.Item1, postResult.Item2);
        }

        /// <summary>
        /// POST方式发送信息(text格式)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="raw">字符串</param>
        /// <param name="headerDict">http头信息</param>
        /// <param name="contentType">内容格式类型，默认为："text/plain"</param>
        /// <returns></returns>
        public static async Task<(HttpStatusCode, string)> PostText(this HttpClient client, string url, string raw, IDictionary<string, string> headerDict)
        {
            var postResult = await client.PostNoForm(url, raw, headerDict, CONTENT_TYPE_TEXT);
            return (postResult.Item1, postResult.Item2);
        }

        /// <summary>
        /// POST方式发送信息(RAW格式)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="raw">字符串</param>
        /// <returns></returns>
        public static async Task<(HttpStatusCode, string, Dictionary<string, List<string>>)> PostJsonGetHeaderDic(this HttpClient client, string url, string raw)
        {
            var postResult = await client.PostNoForm(url, raw, null, CONTENT_TYPE_JSON);
            return postResult;
        }

        /// <summary>
        /// POST方式发送信息(RAW格式)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="raw">字符串</param>
        /// <param name="headerDict">http头信息</param>
        /// <returns></returns>
        public static async Task<(HttpStatusCode, string, Dictionary<string, List<string>>)> PostJsonGetHeaderDic(this HttpClient client, string url, string raw, IDictionary<string, string> headerDict)
        {
            var postResult = await client.PostNoForm(url, raw, headerDict, CONTENT_TYPE_JSON);
            return postResult;
        }

        /// <summary>
        /// POST方式发送信息(RAW格式)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="raw">字符串</param>
        /// <param name="headerDict">http头信息</param>
        /// <param name="contentType">内容格式类型，默认为：application/json</param>
        /// <returns></returns>
        public static async Task<(HttpStatusCode, string, Dictionary<string, List<string>>)> PostNoForm(this HttpClient client, string url, string raw, IDictionary<string, string> headerDict, string contentType)
        {
            string responseText = null;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            Dictionary<string, List<string>> responseHeaderDic = null;
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                if (headerDict?.Count > 0)
                {
                    foreach (KeyValuePair<string, string> pair in headerDict)
                    {
                        request.Headers.Add(pair.Key, pair.Value);
                    }
                }
                request.Content = new StringContent(raw ?? string.Empty, Encoding.UTF8, contentType);

                using (HttpResponseMessage responseMessage = await client.SendAsync(request))
                {
                    statusCode = responseMessage.StatusCode;
                    responseHeaderDic = responseMessage.Headers.ToDictionary(m => m.Key, ele => ele.Value.ToList());
                    try
                    {
                        responseText = await responseMessage.Content.ReadAsStringAsync();
                    }
                    catch (Exception ex)
                    {
                        responseText = $"statusCode:{statusCode},ex:{ex.ToString()}";
                    }
                }
            }
            return (statusCode, responseText, responseHeaderDic);
        }
    }
}
