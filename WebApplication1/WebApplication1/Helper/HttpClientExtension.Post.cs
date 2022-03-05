using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public partial class HttpClientExtension
    {
        public const string CONTENT_TYPE_URLENCODED = "application/x-www-form-urlencoded";

        public const string CONTENT_TYPE_JSON = "application/json";
        /// <summary>
        /// POST方式发送信息(x-www-form-urlencoded字典格式)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="postDict">x-www-form-urlencoded字典格式</param>
        /// <param name="headerDict">http头信息</param>
        /// <param name="contentType">内容格式类型，默认为：application/x-www-form-urlencoded</param>
        /// <returns></returns>
        public static async Task<(HttpStatusCode, string)> Post(this HttpClient client, string url, IDictionary<string, string> postDict, IDictionary<string, string> headerDict, string contentType = CONTENT_TYPE_URLENCODED)
        {
            string responseText = null;
            HttpStatusCode statusCode = HttpStatusCode.OK;
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
                    using (var cancellationToken = new CancellationTokenSource(MILLISECONDS_DELAY))
                    {
                        using (HttpResponseMessage responseMessage = await client.SendAsync(request, cancellationToken.Token))
                        {
                            statusCode = responseMessage.StatusCode;
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
            }
            return (statusCode, responseText);
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
        public static async Task<(HttpStatusCode, string)> Post(this HttpClient client, string url, string raw, string contentType = CONTENT_TYPE_JSON)
        {
            return await client.Post(url, raw, null, contentType);
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
        public static async Task<(HttpStatusCode, string)> Post(this HttpClient client, string url, string raw, IDictionary<string, string> headerDict, string contentType = CONTENT_TYPE_JSON)
        {
            string responseText = null;
            HttpStatusCode statusCode = HttpStatusCode.OK;
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
                using (var cancellationToken = new CancellationTokenSource(MILLISECONDS_DELAY))
                {
                    using (HttpResponseMessage responseMessage = await client.SendAsync(request, cancellationToken.Token))
                    {
                        statusCode = responseMessage.StatusCode;
                        try
                        {
                            responseText = await responseMessage.Content.ReadAsStringAsync();
                        }
                        catch (Exception ex)
                        {
                            responseText = $"statusCode:{statusCode},ex:{ex.ToString()}";
                        }

                        //移除responseMessage.ToString()获取失败消息的方法，该方法会丢失部分错误数据
                        //if (responseMessage.IsSuccessStatusCode)
                        //{
                        //    responseText = await responseMessage.Content.ReadAsStringAsync();
                        //}
                        //else
                        //{
                        //    responseText = responseMessage.ToString();
                        //}
                    }
                }
            }
            return (statusCode, responseText);
        }

        /// <summary>
        /// POST方式发送信息(RAW格式)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="raw">字符串</param>
        /// <param name="headerDict">http头信息</param>
        /// <param name="contentType">内容格式类型，默认为：application/json</param>
        /// <returns>返回值带有返回的cookie</returns>
        public static async Task<(HttpStatusCode, string, string)> PostBackCookie(this HttpClient client, string url, string raw, IDictionary<string, string> headerDict, string contentType = CONTENT_TYPE_JSON)
        {
            string responseText = null;
            string responseSetCookie = null;
            HttpStatusCode statusCode = HttpStatusCode.OK;
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
                using (var cancellationToken = new CancellationTokenSource(MILLISECONDS_DELAY))
                {
                    using (HttpResponseMessage responseMessage = await client.SendAsync(request, cancellationToken.Token))
                    {
                        statusCode = responseMessage.StatusCode;
                        try
                        {
                            responseText = await responseMessage.Content.ReadAsStringAsync();
                            if (responseMessage.Headers != null && responseMessage.Headers.TryGetValues("Set-Cookie", out var cookieList))
                            {
                                if (cookieList != null)
                                {
                                    responseSetCookie = cookieList.FirstOrDefault();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            responseText = $"statusCode:{statusCode},ex:{ex.ToString()}";
                        }

                    }
                }
            }
            return (statusCode, responseText, responseSetCookie);
        }

        /// <summary>
        /// POST方式发送信息(RAW格式)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="raw">字符串</param>
        /// <param name="headerDict">http头信息</param>
        /// <param name="httpMethod">method</param>
        /// <param name="contentType">内容格式类型，默认为：application/json</param>
        /// <returns></returns>
        public static async Task<(HttpStatusCode, string)> SendRequestAsync(this HttpClient client, string url, string raw, HttpMethod httpMethod, IDictionary<string, string> headerDict = null, string contentType = CONTENT_TYPE_JSON)
        {
            string responseText = null;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            using (HttpRequestMessage request = new HttpRequestMessage(httpMethod, url))
            {
                if (headerDict?.Count > 0)
                {
                    foreach (KeyValuePair<string, string> pair in headerDict)
                    {
                        request.Headers.Add(pair.Key, pair.Value);
                    }
                }
                request.Content = new StringContent(raw ?? string.Empty, Encoding.UTF8, contentType);
                using (var cancellationToken = new CancellationTokenSource(MILLISECONDS_DELAY))
                {
                    using (HttpResponseMessage responseMessage = await client.SendAsync(request, cancellationToken.Token))
                    {
                        statusCode = responseMessage.StatusCode;
                        try
                        {
                            responseText = await responseMessage.Content.ReadAsStringAsync();
                        }
                        catch (Exception ex)
                        {
                            responseText = $"statusCode:{statusCode},ex:{ex.ToString()}";
                        }

                        //移除responseMessage.ToString()获取失败消息的方法，该方法会丢失部分错误数据
                        //if (responseMessage.IsSuccessStatusCode)
                        //{
                        //    responseText = await responseMessage.Content.ReadAsStringAsync();
                        //}
                        //else
                        //{
                        //    responseText = responseMessage.ToString();
                        //}
                    }
                }
            }
            return (statusCode, responseText);
        }
        
        /// <summary>
        /// POST方式发送信息(x-www-form-urlencoded字典格式)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="postDict">x-www-form-urlencoded字典格式</param>
        /// <param name="headerDict">http头信息</param>
        /// <param name="contentType">内容格式类型，默认为：application/x-www-form-urlencoded</param>
        /// <returns></returns>
        public static async Task<(HttpStatusCode, string)> PostAsync(this HttpClient client, string url, IDictionary<string, string> postDict, IDictionary<string, string> headerDict = null, string contentType = CONTENT_TYPE_URLENCODED)
        {
            string responseText = null;
            HttpStatusCode statusCode = HttpStatusCode.OK;
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
                    using (var cancellationToken = new CancellationTokenSource(MILLISECONDS_DELAY))
                    {
                        using (HttpResponseMessage responseMessage = await client.SendAsync(request, cancellationToken.Token))
                        {
                            statusCode = responseMessage.StatusCode;
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
            }
            return (statusCode, responseText);
        }
    }
}
