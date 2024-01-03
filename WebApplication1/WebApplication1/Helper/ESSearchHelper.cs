using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebApplication1.Model.PayNotify;

namespace WebApplication1.Helper
{
    public class ESSearchHelper
    {
        protected HttpClient PayHttpClient { get; set; }
        public ILogger Logger { get; set; }

        public ESSearchHelper(IHttpClientFactory httpClientFactory, ILogger<ESSearchHelper> logger)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.Logger = logger;
        }

        /// <summary>
        /// 从ES中获取日志
        /// </summary>
        /// <param name="logFlag"></param>
        /// <param name="esRootDomain"></param>
        /// <param name="dataFilter"></param>
        /// <param name="beginHours"></param>
        /// <param name="logTypeFunc"></param>
        /// <returns></returns>
        public async Task<List<ESLog>> GetESLogList(string logFlag, string esRootDomain, string dataFilter, int beginHours, Func<string, string> logTypeFunc)
        {
            List<ESLog> logList = new List<ESLog>(100);

            //ES搜索文档：https://www.elastic.co/guide/en/elasticsearch/reference/8.1/query-dsl.html
            //ES搜索测试控制台：https://log.meshopstore.com/app/dev_tools#/console
            //ES参考方案可以从：https://log.meshopstore.com/app/discover 获取搜索结构后，再将请求日志中的请求参数中的filter下的参数拿来测试

            var body = @"
{
    ""size"": 2000,
    ""query"": {
        ""bool"": {
            ""must"": [],
            ""filter"": [
                {
                    ""bool"": {
                        ""filter"": " + dataFilter + @"
                    }
                },
                {
                    ""range"": {
                        ""@timestamp"": {
                            ""gte"": """ + DateTime.UtcNow.AddHours(-1 * beginHours).ToString("yyyy-MM-ddTHH:mm:ss") + @""",
                            ""lte"": """ + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") + @""",
                            ""format"": ""strict_date_optional_time""
                        }
                    }
                }
            ],
            ""should"": [],
            ""must_not"": []
        }
    }
}";

            this.Logger.LogInformation($"{logFlag}正在获取查询结果...");

            string postUrl = $"https://log.{esRootDomain}/api/console/proxy?path=logstash-%2A%2F_search&method=GET";
            Dictionary<string, string> postHeaderDic = new Dictionary<string, string>
            {
                {"kbn-xsrf", "true" },
                {"Cookie","fpestid=3321JyHDFhjvOaG4OA-KVmf3BA5U1jOU0Ju3pq7Ry9dDIGgze3gmS2kWR1ojW5HQ6vcgAg; forterToken=5be0fc0308f24c55832ff0d8ba700ac0_1701004934131__UDF43-m4_11ck; _sp_id.36e3=9b6b2b63-7663-4289-b4a5-d7f36e7f9647.1698075342.4.1704295100.1701179456.a47b7116-aa58-4902-b687-9f856d7164bf; sid=Fe26.2**d0b2352835ab029e300f113b6bcfb7178f71c7ace7f888dc3ebd91ac977c2ec8*vglJ4UxHuzG0Durd9VOLtw*rprnTNX8hiBPL1yTV7uIrXWvhUQOXH_xTUwGcxRUAHuZSQ475NccYJEEget5fkuS8SYz6G8y51IEziQruGQ6vpgeve54mNNz7__fdGe_yJa4vW0oLmQYHA1R6echKphkeKs1n0RTh2JQSStx3mcYDGRkVETmpQSIqk3aizaAR8ZzUWF_48MZDhavda1un4LgbAYxWRG_HVBVZ5fRwnc_AORmhWakhmarFMZ6eNTmbifIuMZQuEtH5K6yx4ixDhXV**6c040ee57604e674b09beee73e91e8a1ebbf9fef8daed19ea9843796e4e5ac5b*a5R41UtiqlpeAh0whtEbxOC3HvPScjSECoQ_FIWPwj0" }
            };
            string responseResult2 = (await this.PayHttpClient.PostJson(postUrl, body, headerDict: postHeaderDic)).Item2;
            JArray hitJArray = JObject.Parse(responseResult2).SelectToken("hits.hits")?.ToObject<JArray>();
            foreach (JObject item in hitJArray)
            {
                string log = null;
                if (item.SelectToken("_source.message") != null)
                {
                    log = item.SelectToken("_source.message").ToObject<string>() ?? "";
                }
                else
                {
                    log = item.SelectToken("_source.log").ToObject<string>() ?? "";
                }
                log = log.Replace("\\", "");
                string type = logTypeFunc(log);

                if (!string.IsNullOrEmpty(type))
                {
                    logList.Add(new ESLog
                    {
                        Type = type,
                        Log = log,
                        ESResponse = responseResult2.Length > 32767 ? responseResult2.Substring(0, 32767) : responseResult2
                    });
                }
            }

            return logList;
        }

        /// <summary>
        /// 从ES中获取Nginx相关日志
        /// </summary>
        /// <param name="logFlag"></param>
        /// <param name="esRootDomain"></param>
        /// <param name="dataFilter"></param>
        /// <param name="beginDays"></param>
        /// <param name="logTypeFunc"></param>
        /// <returns></returns>
        public async Task<List<ESLog>> GetESNginxLogList(string logFlag, string esRootDomain, string dataFilter, int beginDays, Func<string, string> logTypeFunc)
        {
            List<ESLog> logList = new List<ESLog>(100);

            //ES搜索文档：https://www.elastic.co/guide/en/elasticsearch/reference/8.1/query-dsl.html
            //ES搜索测试控制台：https://log.meshopstore.com/app/dev_tools#/console
            //ES参考方案可以从：https://log.meshopstore.com/app/discover 获取搜索结构后，再将请求日志中的请求参数中的filter下的参数拿来测试

            string beginDateStr = DateTime.UtcNow.AddDays(-1 * beginDays).ToString("yyyy-MM-ddTHH:mm:ss");
            string endDateStr = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");

#if DEBUG
            beginDateStr = "2022-12-28T20:38:16.694Z";
            endDateStr = "2022-12-28T20:48:53.057Z";
#endif

            var body = @"
{
    ""size"": 8000,
    ""query"": {
        ""bool"": {
            ""must"": [],
            ""filter"": [
                {
                    ""bool"": {
                        ""filter"": " + dataFilter + @"
                    }
                },
                {
                    ""match_phrase"": {
                        ""kubernetes.labels.app_kubernetes_io/name"": ""ingress-nginx""
                    }
                },
                {
                    ""range"": {
                        ""@timestamp"": {
                            ""gte"": """ + beginDateStr + @""",
                            ""lte"": """ + endDateStr + @""",
                            ""format"": ""strict_date_optional_time""
                        }
                    }
                }
            ],
            ""should"": [],
            ""must_not"": []
        }
    }
}";

            this.Logger.LogInformation($"{logFlag}正在获取查询结果...");

            string postUrl = $"https://log.{esRootDomain}/api/console/proxy?path=logstash-%2A%2F_search&method=GET";
            Dictionary<string, string> postHeaderDic = new Dictionary<string, string>
            {
                {"kbn-xsrf", "true" }
            };
            string responseResult2 = (await this.PayHttpClient.PostJson(postUrl, body, headerDict: postHeaderDic)).Item2;
            JArray hitJArray = JObject.Parse(responseResult2).SelectToken("hits.hits")?.ToObject<JArray>();
            foreach (JObject item in hitJArray)
            {
                string log = item.SelectToken("_source.log").ToObject<string>();
                log = log.Replace("\\", "");
                string type = logTypeFunc(log);

                if (!string.IsNullOrEmpty(type))
                {
                    logList.Add(new ESLog
                    {
                        Type = type,
                        Log = log,
                        ESResponse = responseResult2.Length > 32767 ? responseResult2.Substring(0, 32767) : responseResult2
                    });
                }
            }

            return logList;
        }

        /// <summary>
        /// 从文件中获取日志
        /// </summary>
        /// <param name="logFlag"></param>
        /// <param name="filePath"></param>
        /// <param name="logTypeFunc"></param>
        /// <returns></returns>
        public List<ESLog> GetLogListByFile(string logFlag, string filePath, Func<string, string> logTypeFunc)
        {
            List<ESLog> logList = new List<ESLog>(100);

            this.Logger.LogInformation($"{logFlag}正在获取查询结果...");

            string lineText = null;
            using (StreamReader streamReader = new StreamReader(filePath, Encoding.UTF8))
            {
                while ((lineText = streamReader.ReadLine()) != null)
                {
                    lineText = lineText.Replace("\\", "");
                    string type = logTypeFunc(lineText);

                    if (!string.IsNullOrEmpty(type))
                    {
                        logList.Add(new ESLog
                        {
                            Type = type,
                            Log = lineText,
                            ESResponse = lineText.Length > 32767 ? lineText.Substring(0, 32767) : lineText
                        });
                    }
                }
            }

            return logList;
        }
    }

    public class ESLog
    {
        public string Type { get; set; }
        public string Log { get; set; }
        public string ESResponse { get; set; }
    }
}
