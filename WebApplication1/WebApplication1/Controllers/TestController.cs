using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Model;

namespace WebApplication1.Controllers
{
    [Route("api/test")]
    [ApiController]
    public class TestController : ControllerBase
    {
        protected HttpClient PayHttpClient { get; set; }
        public ExcelHelper ExcelHelper { get; set; }
        public IWebHostEnvironment WebHostEnvironment { get; set; }
        public ILogger Logger { get; set; }
        public IConfiguration Configuration { get; set; }

        public TestController(IHttpClientFactory httpClientFactory, ExcelHelper excelHelper, IWebHostEnvironment webHostEnvironment, ILogger<TestController> logger, IConfiguration configuration)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
            this.Logger = logger;
            this.Configuration = configuration;
        }

        /// <summary>
        /// 将enJSON文建转换为EXCEL发给产品进行翻译
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task<IActionResult> BuildEnJsonToExcel()
        {
            string templateName = "Template1";

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

            List<MeshopExcelModel> dataList = new List<MeshopExcelModel>(500);
            int position = 1;
            int totalCount = pageJPropertyList.Count();
            foreach (JObject pageJProperty in pageJPropertyList)
            {
                MeshopExcelModel model = new MeshopExcelModel
                {
                    OrderGuid = pageJProperty.SelectToken("Guid").ToObject<string>(),
                    CreateTime = pageJProperty.SelectToken("CreateTime").ToObject<DateTime>()
                };
                await this.GetOrderPayType(totalCount, position, model);
                dataList.Add(model);
                position++;
            }

            IWorkbook workbook = ExcelHelper.CreateOrUpdateWorkbook(dataList);

            ExcelHelper.SaveWorkbookToFile(workbook, @"C:\Users\lixianghong\Desktop\Test.xlsx");

            return Ok();
        }

        private async Task GetOrderPayType(int totalCount, int position, MeshopExcelModel model)
        {
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
@"                                    ""gte"": ""2022-02-21T04:45:19.219Z"",
" + "\n" +
@"                                    ""lte"": ""2022-03-05T04:45:19.219Z"",
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

            int tryCount = 1;
            do
            {
                try
                {
                    var responseResult1 = await this.PayHttpClient.Post("https://log.meshopstore.com/internal/search/es", body, headerDict: new Dictionary<string, string>
                {
                    {"kbn-version", "7.9.3" }
                });

                    requestID = JObject.Parse(responseResult1.Item2).SelectToken("id")?.ToObject<string>();

                    this.Logger.LogInformation($"正在查询数据第{position}/{totalCount}个,尝试第{tryCount}次查询：{requestID}");
                    tryCount++;
                }
                catch (Exception)
                {
                }

            } while (string.IsNullOrEmpty(requestID));


            body = @"{
" + "\n" +
$@"    ""id"":""{requestID}""
" + "\n" +
@"}";
            string responseResult2 = null;
            do
            {
                try
                {
                    responseResult2 = (await this.PayHttpClient.Post("https://log.meshopstore.com/internal/search/es", body, headerDict: new Dictionary<string, string>
            {
                {"kbn-version", "7.9.3" }
            })).Item2;
                }
                catch (Exception)
                {

                    throw;
                }
            } while (string.IsNullOrEmpty(responseResult2));

            string payType = "无";

            JArray hitJArray = JObject.Parse(responseResult2).SelectToken("rawResponse.hits.hits")?.ToObject<JArray>();
            foreach (JObject item in hitJArray)
            {
                string log = item.SelectToken("_source.log").ToObject<string>();
                model.Content += log + "\n";
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
                if (!string.IsNullOrEmpty(payType))
                {
                    break;
                }
            }
            model.PayType = payType;
        }

    }
}
