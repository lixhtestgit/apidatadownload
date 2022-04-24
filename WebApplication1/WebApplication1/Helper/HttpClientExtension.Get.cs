using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public static partial class HttpClientExtension
    {
        /// <summary>
        /// GET获取HTTP信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="headerDict">http头信息</param>
        /// <returns></returns>
        public static async Task<(HttpStatusCode, string)> Get(this HttpClient client, string url, IDictionary<string, string> headerDict = null)
        {
            string responseText = null;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (headerDict?.Count > 0)
                {
                    foreach (KeyValuePair<string, string> pair in headerDict)
                    {
                        request.Headers.Add(pair.Key, pair.Value);
                    }
                }
                using (var cancellationToken = new CancellationTokenSource(MILLISECONDS_DELAY))
                {
                    using (HttpResponseMessage response = await client.SendAsync(request, cancellationToken.Token))
                    {
                        statusCode = response.StatusCode;
                        try
                        {
                            responseText = await response.Content.ReadAsStringAsync();
                        }
                        catch (Exception ex)
                        {
                            responseText = ex.ToString();
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
    }
}
