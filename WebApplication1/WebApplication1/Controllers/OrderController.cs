using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
using WebApplication1.BIZ;
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
        public ILogger Logger { get; set; }
        public ESSearchHelper ESSearchHelper { get; set; }
        public CheckoutBIZ CheckoutBIZ { get; set; }

        public OrderController(
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            ILogger<TestController> logger,
            ESSearchHelper eSSearchHelper,
            CheckoutBIZ checkoutBIZ)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.Logger = logger;
            this.ESSearchHelper = eSSearchHelper;
            this.CheckoutBIZ = checkoutBIZ;
        }

        /// <summary>
        /// ES搜索订单支付方式
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task<IActionResult> ESSearchOrderPayType()
        {
            string filePath = $@"C:\Users\lixianghong\Desktop\弃单数据_{DateTime.Now.ToString("yyyyMMdd")}.xlsx";
            List<CheckoutOrder> dataList = new List<CheckoutOrder>(1000);

            IWorkbook workbook = null;
            try
            {
                List<CheckoutOrder> hadExportList = ExcelHelper.ReadTitleDataList<CheckoutOrder>(filePath, new ExcelFileDescription());
                dataList.AddRange(hadExportList);
                this.Logger.LogInformation($"已导出数据共{hadExportList.Count}个.");

                DateTime beginDate = Convert.ToDateTime("2021-09-11");
                DateTime endDate = DateTime.Now;
                List<CheckoutOrder> noPayList = await this.CheckoutBIZ.GetList("adoebike.meshopstore.com", "info@aodishi.net", "Adoebike2021", beginDate, endDate, 0);

                //前368个重新查询，查询时间错误
                int position = dataList.Count + 1;
                int totalCount = noPayList.Count;
                int days = (int)(DateTime.Now - beginDate).TotalDays + 3;
                DateTime esMinDate = Convert.ToDateTime("2022-04-01").AddHours(-8);
                foreach (CheckoutOrder checkoutOrder in noPayList)
                {
                    if (!dataList.Exists(m => m.CheckoutGuid == checkoutOrder.CheckoutGuid))
                    {
                        if (checkoutOrder.CreateTime > esMinDate)
                        {
                            await this.UpdateOrderPayData(totalCount, position, checkoutOrder, days);
                        }
                        else
                        {
                            this.Logger.LogInformation($"第{position}/{totalCount}个支付类型数据ES日志已清理,无法查询...");
                        }
                        dataList.Add(checkoutOrder);
                        position++;
                    }
                }

                workbook = ExcelHelper.CreateOrUpdateWorkbook(dataList);
                ExcelHelper.SaveWorkbookToFile(workbook, filePath);
            }
            catch (Exception e)
            {
                workbook = ExcelHelper.CreateOrUpdateWorkbook(dataList);
                ExcelHelper.SaveWorkbookToFile(workbook, filePath);

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
        private async Task UpdateOrderPayData(int totalCount, int position, CheckoutOrder model, int lastDays)
        {
            #region 1-获取支付类型

            string dataFilter = @"[
    {
        ""multi_match"": {
            ""type"": ""phrase"",
            ""query"": """ + model.CheckoutGuid + @""",
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

            if (esLogList.Count > 0)
            {
                model.ESPayTypeList.AddRange(esLogList.Select(m => m.Type));
            }

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
                                    ""query"": """ + model.CheckoutGuid + @""",
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
                        sessionID = new Regex("(?<=\"token\":\")[^\"]+(?=\")").Match(log).Value;
                        if (!string.IsNullOrEmpty(sessionID) && !sessionIDList.Contains(sessionID))
                        {
                            sessionIDList.Add(sessionID);
                        }
                    }
                    return sessionID;
                });
                model.SessionIDList.AddRange(sessionIDList);

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
                        string payError = null;
                        if (log.Contains("CreateOrder_Result", StringComparison.OrdinalIgnoreCase))
                        {
                            //获取创建订单结果日志
                            validLog = log;
                            model.ESCreateOrderResultLogList.Add(validLog);

                            if (model.ESPayType.Contains("PayPal"))
                            {
                                payError = new Regex("(?<=\"issue\":\")[^\"]+(?=\")").Match(log).Value;
                                if (!string.IsNullOrEmpty(payError))
                                {
                                    model.CreateOrderErrorReasonList.Add(payError);
                                }
                            }
                        }
                        else
                        {
                            //获取支付结果日志
                            if (model.ESPayType.Contains("PayPal")
                                && log.Contains("PP_4002_CaptureOrder_Result", StringComparison.OrdinalIgnoreCase))
                            {
                                //获取创建订单结果日志
                                validLog = log;
                                model.ESPayResultLogList.Add(validLog);

                                payError = new Regex("(?<=\"issue\":\")[^\"]+(?=\")").Match(log).Value;
                                if (!string.IsNullOrEmpty(payError))
                                {
                                    model.PayErrorReasonList.Add(payError);
                                }
                            }
                            else if (model.ESPayType.Contains("PayEase直连")
                                && log.Contains("PayEaseDirect_v1Controller_ResultPage", StringComparison.OrdinalIgnoreCase))
                            {
                                //获取创建订单结果日志
                                validLog = log;
                                model.ESPayResultLogList.Add(validLog);

                                payError = new Regex("(?<=\"orderInfo\":\")[^\"]+(?=\")").Match(log).Value;
                                if (!string.IsNullOrEmpty(payError))
                                {
                                    model.PayErrorReasonList.Add(payError);
                                }
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
                                    ""query"": """ + model.CheckoutGuid + @""",
                                    ""lenient"": true
                                }
                            }
                        ]";

                esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个弃单日志数据", dataFilter, lastDays, log =>
                {
                    string validLog = null;
                    string payError = null;

                    //首信易三方
                    if (model.ESPayType.Contains("PayEase三方或者本地化"))
                    {
                        if (log.Contains("PayEase_1002_CreateOrder_Result", StringComparison.OrdinalIgnoreCase))
                        {
                            //获取创建订单结果日志
                            validLog = log;
                            model.ESCreateOrderResultLogList.Add(validLog);

                            payError = new Regex("(?<=\"orderInfo\":\")[^\"]+(?=\")").Match(log).Value;
                            if (!string.IsNullOrEmpty(payError))
                            {
                                model.PayErrorReasonList.Add(payError);
                            }
                        }
                        if (log.Contains("PayEase_1003_CreateOrder_CallBack_Result", StringComparison.OrdinalIgnoreCase))
                        {
                            //获取创建订单结果日志
                            validLog = log;
                            model.ESPayResultLogList.Add(validLog);

                            payError = new Regex("(?<=\"orderInfo\":\")[^\"]+(?=\")").Match(log).Value;
                            if (!string.IsNullOrEmpty(payError))
                            {
                                model.PayErrorReasonList.Add(payError);
                            }
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
        }
    }
}
