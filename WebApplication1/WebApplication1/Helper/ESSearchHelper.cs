using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

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

        public async Task<List<ESLog>> GetESLogList(string logFlag, string dataFilter, int beginDays, Func<string, string> logTypeFunc)
        {
            //想要快速查询：需要打开ES搜索页，放置一个定时器定时输入耗时较长的搜索词点击查询按钮，程序方可快速查询数据
            //window.setInterval(function(){document.querySelector("button.euiSuperUpdateButton").click();},2000)
            //搜索内容：挑选耗时比较长的搜索词，让ES处于搜索中状态，如："a" and ("/ajax/paydd" or "/ajax/pay")  
            //搜索时间：可选定2个月/年等较长时间段，让ES处于搜索中状态
            //下面查询语句注意搜索时间段


            List<ESLog> logList = new List<ESLog>(100);

            #region 获取requestID

            var body = @"
{
    ""params"": {
        ""ignoreThrottled"": true,
        ""index"": ""logstash-*"",
        ""body"": {
            ""version"": true,
            ""size"": 500,
            ""sort"": [
                {
                    ""@timestamp"": {
                        ""order"": ""desc"",
                        ""unmapped_type"": ""boolean""
                    }
                }
            ],
            ""aggs"": {
                ""2"": {
                    ""date_histogram"": {
                        ""field"": ""@timestamp"",
                        ""fixed_interval"": ""1h"",
                        ""time_zone"": ""Asia/Shanghai"",
                        ""min_doc_count"": 1
                    }
                }
            },
            ""stored_fields"": [
                ""*""
            ],
            ""script_fields"": {},
            ""docvalue_fields"": [
                {
                    ""field"": ""@timestamp"",
                    ""format"": ""date_time""
                },
                {
                    ""field"": ""t.$date"",
                    ""format"": ""date_time""
                },
                {
                    ""field"": ""timestamp"",
                    ""format"": ""date_time""
                }
            ],
            ""_source"": {
                ""excludes"": []
            },
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
                                    ""gte"": """ + DateTime.UtcNow.AddDays(-1 * beginDays).ToString("yyyy-MM-ddTHH:mm:ss") + @""",
                                    ""lte"": """ + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") + @""",
                                    ""format"": ""strict_date_optional_time""
                                }
                            }
                        }
                    ],
                    ""should"": [],
                    ""must_not"": []
                }
            },
            ""highlight"": {
                ""pre_tags"": [
                    ""@kibana-highlighted-field@""
                ],
                ""post_tags"": [
                    ""@/kibana-highlighted-field@""
                ],
                ""fields"": {
                    ""*"": {}
                },
                ""fragment_size"": 2147483647
            }
        },
        ""rest_total_hits_as_int"": true,
        ""ignore_unavailable"": true,
        ""ignore_throttled"": true,
        ""preference"": 1649829310063,
        ""timeout"": ""30000ms""
    }
}";
            #endregion

            string requestID = null;

            int trySearchIDCount = 1;
            do
            {
                try
                {
                    var responseResult1 = await this.PayHttpClient.Post("https://log.meshopstore.com/internal/search/es", body, headerDict: new Dictionary<string, string>
                {
                    {"kbn-version", "7.9.3" }
                });

                    requestID = JObject.Parse(responseResult1.Item2).SelectToken("id")?.ToObject<string>();

                    this.Logger.LogInformation($"{logFlag}正在获取查询ID,尝试第{trySearchIDCount}次查询：{requestID}");
                    trySearchIDCount++;
                }
                catch (Exception)
                {
                }

            } while (string.IsNullOrEmpty(requestID));


            int trySearchResultCount = 1;
            body = @"{
                        ""id"":""" + requestID + @"""
                    }";
            string responseResult2 = null;
            bool isRuning = false;
            do
            {
                try
                {
                    isRuning = true;
                    responseResult2 = (await this.PayHttpClient.Post("https://log.meshopstore.com/internal/search/es", body, headerDict: new Dictionary<string, string>
                    {
                        {"kbn-version", "7.9.3" }
                    })).Item2;
                    isRuning = JObject.Parse(responseResult2).SelectToken("is_running")?.ToObject<bool>() ?? false;
                }
                catch (Exception)
                {
                    throw;
                }
                if (isRuning)
                {
                    this.Logger.LogInformation($"{logFlag}正在读取查询结果,ES正在运行中,尝试第{trySearchResultCount}次查询：{requestID}");
                }
                trySearchResultCount++;
            } while (string.IsNullOrEmpty(responseResult2) || isRuning);

            JArray hitJArray = JObject.Parse(responseResult2).SelectToken("rawResponse.hits.hits")?.ToObject<JArray>();
            foreach (JObject item in hitJArray)
            {
                string log = item.SelectToken("_source.log").ToObject<string>();
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
    }

    public class ESLog
    {
        public string Type { get; set; }
        public string Log { get; set; }
        public string ESResponse { get; set; }
    }
}
