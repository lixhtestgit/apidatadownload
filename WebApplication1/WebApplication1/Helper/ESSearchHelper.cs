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
            this.PayHttpClient.Timeout = TimeSpan.FromSeconds(300);
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
        public async Task<List<ESLog>> GetESLogList(string logFlag, string esRootDomain, string dataFilter, DateTime utcBeginDate, DateTime utcEndDate, Func<string, string> logTypeFunc)
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
                            ""gte"": """ + utcBeginDate.ToString("yyyy-MM-ddTHH:mm:ss") + @""",
                            ""lte"": """ + utcEndDate.ToString("yyyy-MM-ddTHH:mm:ss") + @""",
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
                {"Cookie","_gcl_au=1.1.942372824.1713955206; _fbp=fb.1.1713955207447.932788562; fpestid=RdOPKQLD26tbLJGb9S3Fcn0jUBcN9TtTfpYI98kjxkIHat23_PlaNMKKQHgvLtW7JArGkg; _cc_id=e8b6345ed8eccf81314ebd32ec946bb7; _sp_id.da82=f6ea6b3f-f5ae-4caa-bf74-ddfdc3cd5452.1713955206.22.1719303944.1719300365.8a1282c1-29ac-4f16-9d95-4589de8abee7; ph_phc_FK3gUI6MTOHJ6IIqb519luwpYPYC3ZYm3hGC5KpEQU8_posthog=%7B%22distinct_id%22%3A%2201901466-63f7-761d-8431-bc74437e8f83%22%2C%22%24sesid%22%3A%5B1719303946474%2C%2201904e6b-f6ac-7515-99f4-129f8a6b04a7%22%2C1719302616748%5D%7D; sid=Fe26.2**a28b2361933d039c7b4fb758659a2370123591cc897be79d44ee68bf5e41e469*U_zb1Lzt23ArujpVn8X0vQ*a9knsAqap0P0zmiP9k_XfCgix2dUiNVapacQLqdyHEVZVz3FgCh6uhz1QsAsgPzcNDnT0pOP-MGIBjWeX44dPWMwm73_EsOu0Csg4IVONpuRN7iNmTzN6ZzCpOkvBJmB7KTB0ckjUqkYnWi96J351yiYN5a2pzIyCiDwPYN486_oTE1c05VcO3wSdWa8A8eqdi_0a9kmUzfFZ4N5cqh6aRO_rs9uUDcvXxROtpZCjxmBTDLh4HkhZcqTn7Y5yDmf**eb40772a22f48b5e5562beff24fa9b8306979c13e3d0f962e5a757a57ef89e81*0g6VCpxScA4Vw5mLnzbeWEENOVGyvAIx1B2BKerngBg" }
            };
            //string responseResult2 = (await this.PayHttpClient.PostJson(postUrl, body, headerDict: postHeaderDic)).Item2;
            string responseResult2 = "";
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

                DateTime logTime = item.SelectToken("_source.@timestamp").ToObject<DateTime>();

                if (!string.IsNullOrEmpty(type))
                {
                    logList.Add(new ESLog
                    {
                        Type = type,
                        Log = log,
                        LogTime = logTime,
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

        public DateTime LogTime { get; set; }
    }
}
