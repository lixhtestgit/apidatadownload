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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebApplication1.Enum;
using WebApplication1.Helper;
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
        public ESSearchHelper ESSearchHelper { get; set; }

        public OrderController(
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            ILogger<TestController> logger,
            IConfiguration configuration,
            ESSearchHelper eSSearchHelper)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
            this.Logger = logger;
            this.Configuration = configuration;
            this.ESSearchHelper = eSSearchHelper;
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
                int payStatus = 0;
                foreach (JObject pageJProperty in pageJPropertyList)
                {
                    orderGuid = pageJProperty.SelectToken("Guid").ToObject<string>();
                    payStatus = pageJProperty.SelectToken("State").ToObject<int>();
                    if (true || !dataList.Exists(m => m.OrderGuid == orderGuid))
                    {
                        Order model = new Order
                        {
                            OrderGuid = orderGuid,
                            PayStatus = ((EOrder.State)payStatus).ToString(),
                            CreateTime = pageJProperty.SelectToken("CreateTime").ToObject<DateTime>()
                        };
                        await this.UpdateOrderPayData(totalCount, position, model, 3);
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

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        /// <summary>
        /// 获取支付相关数据
        /// </summary>
        /// <param name="totalCount"></param>
        /// <param name="position"></param>
        /// <param name="model"></param>
        /// <param name="lastDays"></param>
        /// <returns></returns>
        private async Task UpdateOrderPayData(int totalCount, int position, Order model, int lastDays)
        {
            #region 1-获取支付类型

            string dataFilter = @"[
    {
        ""multi_match"": {
            ""type"": ""phrase"",
            ""query"": """ + model.OrderGuid + @""",
            ""lenient"": true
        }
    },
    {
        ""bool"": {
            ""should"": [
                {
                    ""multi_match"": {
                        ""type"": ""phrase"",
                        ""query"": ""/ajax/paydd"",
                        ""lenient"": true
                    }
                },
                {
                    ""multi_match"": {
                        ""type"": ""phrase"",
                        ""query"": ""/ajax/pay"",
                        ""lenient"": true
                    }
                }
            ],
            ""minimum_should_match"": 1
        }
    }
]";

            List<ESLog> esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个支付类型数据", dataFilter, lastDays, log =>
            {
                string payType = null;
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
                return payType;
            });

            ESLog firstLog = esLogList.FirstOrDefault();
            model.ESPayType = firstLog?.Type ?? "无";

            #endregion

            #region 调度版本
            if (model.ESPayType.Contains("PayPal")
                || model.ESPayType.Contains("PayEase直连"))
            {
                #region 2-获取会话ID

                dataFilter = @"[
                            {
                                ""multi_match"": {
                                    ""type"": ""phrase"",
                                    ""query"": """ + model.OrderGuid + @""",
                                    ""lenient"": true
                                }
                            },
                            {
                                ""multi_match"": {
                                    ""type"": ""best_fields"",
                                    ""query"": ""token"",
                                    ""lenient"": true
                                }
                            }
                        ]";
                List<string> sessionIDList = new List<string>(10);

                esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个SessionID数据", dataFilter, lastDays, log =>
                {
                    string sessionID = null;
                    if (log.Contains("token", StringComparison.OrdinalIgnoreCase))
                    {
                        sessionID = new Regex("(?<=\"token\":\")[a-z0-9]+(?=\")").Match(log).Value;
                        if (!string.IsNullOrEmpty(sessionID) && !sessionIDList.Contains(sessionID))
                        {
                            sessionIDList.Add(sessionID);
                            model.SessionIDArrayStr = string.Join(",", sessionIDList);
                        }
                    }
                    return sessionID;
                });

                #endregion

                #region 获取回话结果日志

                foreach (var sessionID in sessionIDList)
                {
                    dataFilter = @"[
                                {
                                    ""multi_match"": {
                                        ""type"": ""best_fields"",
                                        ""query"": """ + sessionID + @""",
                                        ""lenient"": true
                                    }
                                }
                            ]";
                    esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个创建订单结果数据", dataFilter, lastDays, log =>
                    {
                        string validLog = null;
                        if (log.Contains("CreateOrder_Result", StringComparison.OrdinalIgnoreCase))
                        {
                            //获取创建订单结果日志
                            validLog = log;
                            model.ESCreateOrderResultLog += validLog + "\n";
                        }
                        else
                        {
                            //获取支付结果日志
                            if (model.ESPayType.Contains("PayPal")
                                && log.Contains("PP_4002_CaptureOrder_Result", StringComparison.OrdinalIgnoreCase))
                            {
                                //获取创建订单结果日志
                                validLog = log;
                                model.ESPayResultLog += validLog + "\n";
                            }
                            else if (model.ESPayType.Contains("PayEase直连")
                                && log.Contains("PayEaseDirect_v1Controller_ResultPage", StringComparison.OrdinalIgnoreCase))
                            {
                                //获取创建订单结果日志
                                validLog = log;
                                model.ESPayResultLog += validLog + "\n";
                            }
                        }
                        return validLog;
                    });

                }

                #endregion
            }
            #endregion
            #region 非调度版本查询
            else
            {
                #region 2-获取弃单日志列表

                dataFilter = @"[
                            {
                                ""multi_match"": {
                                    ""type"": ""phrase"",
                                    ""query"": """ + model.OrderGuid + @""",
                                    ""lenient"": true
                                }
                            }
                        ]";

                esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个弃单日志数据", dataFilter, lastDays, log =>
                {
                    string validLog = null;

                    //首信易三方
                    if (model.ESPayType.Contains("PayEase三方或者本地化"))
                    {
                        if (log.Contains("PayEase_1002_CreateOrder_Result", StringComparison.OrdinalIgnoreCase))
                        {
                            //获取创建订单结果日志
                            validLog = log;
                            model.ESCreateOrderResultLog += validLog + "\n";
                        }
                        if (log.Contains("PayEase_1003_CreateOrder_CallBack_Result", StringComparison.OrdinalIgnoreCase))
                        {
                            //获取创建订单结果日志
                            validLog = log;
                            model.ESPayResultLog += validLog + "\n";
                        }

                    }
                    else if (true)
                    {
                        //...
                    }

                    return validLog;
                });

                #endregion
            }
            #endregion


            if (string.IsNullOrEmpty(model.ESCreateOrderResultLog))
            {
                model.ESCreateOrderResultLog = "无";
            }
            if (string.IsNullOrEmpty(model.ESPayResultLog))
            {
                model.ESPayResultLog = "无";
            }
        }
    }
}
