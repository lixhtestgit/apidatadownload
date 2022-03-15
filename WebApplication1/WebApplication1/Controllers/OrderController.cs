using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.Model;

namespace WebApplication1.Controllers
{
    [Route("api/Order")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        protected HttpClient PayHttpClient { get; set; }
        public ExcelHelper ExcelHelper { get; set; }
        public IWebHostEnvironment WebHostEnvironment { get; set; }
        public ILogger Logger { get; set; }
        public IConfiguration Configuration { get; set; }

        public OrderController(IHttpClientFactory httpClientFactory, ExcelHelper excelHelper, IWebHostEnvironment webHostEnvironment, ILogger<TestController> logger, IConfiguration configuration)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
            this.Logger = logger;
            this.Configuration = configuration;
        }

        /// <summary>
        /// ES搜索订单支付方式
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task<IActionResult> ESSearchOrderPayType()
        {
            string templateName = "Template1";

            string filePath = @"C:\Users\lixianghong\Desktop\Test.xlsx";
            List<Order> dataList = new List<Order>(1000);

            IWorkbook workbook = null;
            try
            {
                IFileProvider fileProvider = this.WebHostEnvironment.ContentRootFileProvider;
                IFileInfo fileInfo = fileProvider.GetFileInfo($"en-{templateName}.json");

                string fileContent = null;
                using (StreamReader readSteam = new StreamReader(fileInfo.CreateReadStream()))
                {
                    fileContent = await readSteam.ReadToEndAsync();
                }
                JObject templateFileJObj = JObject.Parse(fileContent);
                JArray pageJPropertyList = templateFileJObj.SelectToken("data.Results").ToObject<JArray>();

                if (pageJPropertyList.Count() == 0)
                {
                    throw new Exception($"未找到MyData_{templateName}的配置数据");
                }

                dataList.AddRange(ExcelHelper.ReadTitleDataList<Order>(filePath, new ExcelFileDescription()));
                this.Logger.LogInformation($"已导出数据共{dataList.Count}个.");
                //前368个重新查询，查询时间错误
                int position = dataList.Count + 1;
                int totalCount = pageJPropertyList.Count();
                string orderGuid;
                foreach (JObject pageJProperty in pageJPropertyList)
                {
                    orderGuid = pageJProperty.SelectToken("Guid").ToObject<string>();
                    if (!dataList.Exists(m => m.OrderGuid == orderGuid))
                    {
                        Order model = new Order
                        {
                            OrderGuid = orderGuid,
                            CreateTime = pageJProperty.SelectToken("CreateTime").ToObject<DateTime>()
                        };
                        await this.GetOrderPayType(totalCount, position, model);
                        dataList.Add(model);
                        position++;

                        workbook = ExcelHelper.CreateOrUpdateWorkbook(dataList);
                        ExcelHelper.SaveWorkbookToFile(workbook, filePath);
                    }
                }
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, $"数据收集遇到异常,正在保存数据，请重新收集...");
            }
            return Ok();
        }

        private async Task GetOrderPayType(int totalCount, int position, Order model)
        {
            //想要快速查询：需要打开ES搜索页，放置一个定时器定时输入耗时较长的搜索词点击查询按钮，程序方可快速查询数据
            //window.setInterval(function(){document.querySelector("button.euiSuperUpdateButton").click();},2000)
            //搜索内容：挑选耗时比较长的搜索词，让ES处于搜索中状态，如："a" and ("/ajax/paydd" or "/ajax/pay")
            //下面查询语句注意搜索时间段

            #region 获取requestID

            var body = @"{
" + "\n" +
@"    ""params"": {
" + "\n" +
@"        ""ignoreThrottled"": true,
" + "\n" +
@"        ""index"": ""logstash-*"",
" + "\n" +
@"        ""body"": {
" + "\n" +
@"            ""version"": true,
" + "\n" +
@"            ""size"": 500,
" + "\n" +
@"            ""sort"": [
" + "\n" +
@"                {
" + "\n" +
@"                    ""@timestamp"": {
" + "\n" +
@"                        ""order"": ""desc"",
" + "\n" +
@"                        ""unmapped_type"": ""boolean""
" + "\n" +
@"                    }
" + "\n" +
@"                }
" + "\n" +
@"            ],
" + "\n" +
@"            ""aggs"": {
" + "\n" +
@"                ""2"": {
" + "\n" +
@"                    ""date_histogram"": {
" + "\n" +
@"                        ""field"": ""@timestamp"",
" + "\n" +
@"                        ""fixed_interval"": ""3h"",
" + "\n" +
@"                        ""time_zone"": ""Asia/Shanghai"",
" + "\n" +
@"                        ""min_doc_count"": 1
" + "\n" +
@"                    }
" + "\n" +
@"                }
" + "\n" +
@"            },
" + "\n" +
@"            ""stored_fields"": [
" + "\n" +
@"                ""*""
" + "\n" +
@"            ],
" + "\n" +
@"            ""script_fields"": {},
" + "\n" +
@"            ""docvalue_fields"": [
" + "\n" +
@"                {
" + "\n" +
@"                    ""field"": ""@timestamp"",
" + "\n" +
@"                    ""format"": ""date_time""
" + "\n" +
@"                },
" + "\n" +
@"                {
" + "\n" +
@"                    ""field"": ""t.$date"",
" + "\n" +
@"                    ""format"": ""date_time""
" + "\n" +
@"                },
" + "\n" +
@"                {
" + "\n" +
@"                    ""field"": ""timestamp"",
" + "\n" +
@"                    ""format"": ""date_time""
" + "\n" +
@"                }
" + "\n" +
@"            ],
" + "\n" +
@"            ""_source"": {
" + "\n" +
@"                ""excludes"": []
" + "\n" +
@"            },
" + "\n" +
@"            ""query"": {
" + "\n" +
@"                ""bool"": {
" + "\n" +
@"                    ""must"": [],
" + "\n" +
@"                    ""filter"": [
" + "\n" +
@"                        {
" + "\n" +
@"                            ""bool"": {
" + "\n" +
@"                                ""filter"": [
" + "\n" +
@"                                    {
" + "\n" +
@"                                        ""multi_match"": {
" + "\n" +
@"                                            ""type"": ""phrase"",
" + "\n" +
$@"                                            ""query"": ""{model.OrderGuid}"",
" + "\n" +
@"                                            ""lenient"": true
" + "\n" +
@"                                        }
" + "\n" +
@"                                    },
" + "\n" +
@"                                    {
" + "\n" +
@"                                        ""bool"": {
" + "\n" +
@"                                            ""should"": [
" + "\n" +
@"                                                {
" + "\n" +
@"                                                    ""multi_match"": {
" + "\n" +
@"                                                        ""type"": ""phrase"",
" + "\n" +
@"                                                        ""query"": ""/ajax/pay"",
" + "\n" +
@"                                                        ""lenient"": true
" + "\n" +
@"                                                    }
" + "\n" +
@"                                                },
" + "\n" +
@"                                                {
" + "\n" +
@"                                                    ""multi_match"": {
" + "\n" +
@"                                                        ""type"": ""phrase"",
" + "\n" +
@"                                                        ""query"": ""/ajax/paydd"",
" + "\n" +
@"                                                        ""lenient"": true
" + "\n" +
@"                                                    }
" + "\n" +
@"                                                }
" + "\n" +
@"                                            ],
" + "\n" +
@"                                            ""minimum_should_match"": 1
" + "\n" +
@"                                        }
" + "\n" +
@"                                    }
" + "\n" +
@"                                ]
" + "\n" +
@"                            }
" + "\n" +
@"                        },
" + "\n" +
@"                        {
" + "\n" +
@"                            ""range"": {
" + "\n" +
@"                                ""@timestamp"": {
" + "\n" +
@"                                    ""gte"": ""2021-12-15T07:35:45.109Z"",
" + "\n" +
@"                                    ""lte"": ""2022-03-15T07:35:45.109Z"",
" + "\n" +
@"                                    ""format"": ""strict_date_optional_time""
" + "\n" +
@"                                }
" + "\n" +
@"                            }
" + "\n" +
@"                        }
" + "\n" +
@"                    ],
" + "\n" +
@"                    ""should"": [],
" + "\n" +
@"                    ""must_not"": []
" + "\n" +
@"                }
" + "\n" +
@"            },
" + "\n" +
@"            ""highlight"": {
" + "\n" +
@"                ""pre_tags"": [
" + "\n" +
@"                    ""@kibana-highlighted-field@""
" + "\n" +
@"                ],
" + "\n" +
@"                ""post_tags"": [
" + "\n" +
@"                    ""@/kibana-highlighted-field@""
" + "\n" +
@"                ],
" + "\n" +
@"                ""fields"": {
" + "\n" +
@"                    ""*"": {}
" + "\n" +
@"                },
" + "\n" +
@"                ""fragment_size"": 2147483647
" + "\n" +
@"            }
" + "\n" +
@"        },
" + "\n" +
@"        ""rest_total_hits_as_int"": true,
" + "\n" +
@"        ""ignore_unavailable"": true,
" + "\n" +
@"        ""ignore_throttled"": true,
" + "\n" +
@"        ""preference"": 1646450551014,
" + "\n" +
@"        ""timeout"": ""30000ms""
" + "\n" +
@"    }
" + "\n" +
@"}";
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

                    this.Logger.LogInformation($"正在查询数据第{position}/{totalCount}个查询ID,尝试第{trySearchIDCount}次查询：{requestID}");
                    trySearchIDCount++;
                }
                catch (Exception)
                {
                }

            } while (string.IsNullOrEmpty(requestID));


            int trySearchResultCount = 1;
            body = @"{
" + "\n" +
$@"    ""id"":""{requestID}""
" + "\n" +
@"}";
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
                    this.Logger.LogInformation($"正在查询数据第{position}/{totalCount}个查询结果,ES正在运行中,尝试第{trySearchResultCount}次查询：{requestID}");
                }
                trySearchResultCount++;
            } while (string.IsNullOrEmpty(responseResult2) || isRuning);
            model.Content = responseResult2.Length > 32767 ? responseResult2.Substring(0, 32767) : responseResult2;

            string payType = "无";

            JArray hitJArray = JObject.Parse(responseResult2).SelectToken("rawResponse.hits.hits")?.ToObject<JArray>();
            foreach (JObject item in hitJArray)
            {
                string log = item.SelectToken("_source.log").ToObject<string>();
                if (log.Contains("/ajax/paydd/FPP", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "PayPal快捷";
                }
                else if (log.Contains("/ajax/paydd/PP", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "PayPal";
                }
                else if (log.Contains("/ajax/paydd/PayEaseDirect", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "PayEase直连";
                }
                else if (log.Contains("/ajax/pay/PayEase", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "PayEase三方或者本地化";
                }
                else if (log.Contains("/ajax/paydd/", StringComparison.OrdinalIgnoreCase)
                    || log.Contains("/ajax/pay/", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "其他支付方式+" + log;
                }
                if (!string.IsNullOrEmpty(payType))
                {
                    break;
                }
            }
            model.PayType = payType;
        }
    }
}
