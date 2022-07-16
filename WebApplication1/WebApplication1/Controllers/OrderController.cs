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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebApplication1.BIZ;
using WebApplication1.Helper;
using WebApplication1.Model;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 获取店铺弃单数据并获取对应支付方式日志
    /// </summary>
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

                string shopDomain = "ikebshop.meshopstore.com";
                DateTime beginDate = Convert.ToDateTime("2022-07-13 02:00:00");
                DateTime endDate = Convert.ToDateTime("2022-07-13 09:00:00");
                List<CheckoutOrder> noPayList = await this.CheckoutBIZ.GetList(shopDomain, "lixianghong@meshop.net", "Aa123456", beginDate, endDate, 0);
                string shopRootDomain = shopDomain.Substring(shopDomain.IndexOf('.') + 1, shopDomain.Length - (shopDomain.IndexOf('.') + 1));

                #region A-通过ES更新查询支付方式

                if (true)
                {
                    //前368个重新查询，查询时间错误
                    int position = dataList.Count + 1;
                    int totalCount = noPayList.Count;
                    int days = (int)(DateTime.Now - beginDate).TotalDays + 7;
                    DateTime esMinDate = beginDate.AddDays(-7).AddHours(-8);
                    foreach (CheckoutOrder checkoutOrder in noPayList)
                    {
                        if (!dataList.Exists(m => m.CheckoutGuid == checkoutOrder.CheckoutGuid))
                        {
                            //查询支付方式
                            if (checkoutOrder.CreateTime > esMinDate)
                            {
                                await this.UpdateOrderPayDataByES(totalCount, position, shopRootDomain, checkoutOrder, days);
                            }
                            else
                            {
                                this.Logger.LogInformation($"第{position}/{totalCount}个支付类型数据ES日志已清理,无法查询...");
                            }
                            dataList.Add(checkoutOrder);
                            position++;
                        }
                    }
                }

                #endregion

                #region 通过日志文件查询支付数据

                if (false)
                {
                    noPayList.RemoveAll(m => hadExportList.Exists(em => em.CheckoutGuid == m.CheckoutGuid));
                    this.UpdateOrderPayDataByFile(@"C:\Users\lixianghong\Desktop\pay.log", noPayList);
                }

                #endregion

                if (dataList.Count == 0)
                {
                    dataList.AddRange(noPayList);
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
        /// <param name="esRootDomain"></param>
        /// <param name="model"></param>
        /// <param name="lastDays"></param>
        /// <returns></returns>
        private async Task UpdateOrderPayDataByES(int totalCount, int position, string esRootDomain, CheckoutOrder model, int lastDays)
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

            List<ESLog> esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个支付类型数据", esRootDomain, dataFilter, lastDays, log =>
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
                else if (log.Contains("/ajax/pay/PacyPay", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "PacyPay直连";
                }
                else if (log.Contains("/ajax/paydd/Paytm", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "Paytm";
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
                || model.ESPayType.Contains("PayEase直连")
                || model.ESPayType.Contains("Paytm"))
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
                esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个SessionID数据", esRootDomain, dataFilter, lastDays, log =>
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

                    esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个创建订单结果数据", esRootDomain, dataFilter, lastDays, log =>
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
                            }
                            else if (model.ESPayType.Contains("Paytm") && !validLog.Contains("Success"))
                            {
                                payError = new Regex("(?<=\"resultMsg\":\")[^\"]+(?=\")").Match(log).Value;
                            }
                            if (!string.IsNullOrEmpty(payError))
                            {
                                model.CreateOrderResultList.Add("失败：" + payError);
                            }
                            else
                            {
                                model.CreateOrderResultList.Add("成功");
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
                            }
                            else if (model.ESPayType.Contains("PayEase直连")
                                && log.Contains("PayEaseDirect_v1Controller_ResultPage", StringComparison.OrdinalIgnoreCase))
                            {
                                //获取创建订单结果日志
                                validLog = log;
                                model.ESPayResultLogList.Add(validLog);

                                payError = new Regex("(?<=\"orderInfo\":\")[^\"]+(?=\")").Match(log).Value;
                            }
                            else if (model.ESPayType.Contains("Paytm")
                                && log.Contains("Paytm_2002_GetOrder_Result", StringComparison.OrdinalIgnoreCase))
                            {
                                //获取创建订单结果日志
                                validLog = log;
                                model.ESPayResultLogList.Add(validLog);

                                if (!validLog.Contains("PENDING")
                                    && !validLog.Contains("TXN_SUCCESS"))
                                {
                                    payError = new Regex("(?<=\"resultMsg\":\")[^\"]+(?=\")").Match(log).Value;
                                }
                            }
                            if (!string.IsNullOrEmpty(payError))
                            {
                                model.PayResultList.Add("失败：" + payError);
                            }
                            else
                            {
                                model.PayResultList.Add("成功");
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

                esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个弃单日志数据", esRootDomain, dataFilter, lastDays, log =>
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
                                model.PayResultList.Add("失败：" + payError);
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
                                model.PayResultList.Add("失败：" + payError);
                            }
                        }

                    }
                    else if (model.ESPayType.Contains("PacyPay直连"))
                    {
                        if (log.Contains("PacyPay_1002_CreateOrder_Result", StringComparison.OrdinalIgnoreCase))
                        {
                            //获取创建订单结果日志
                            validLog = log;
                            model.ESCreateOrderResultLogList.Add(validLog);

                            payError = new Regex("(?<=@orderInfo:)[^@]+(?=@)").Match(log).Value;
                            if (!string.IsNullOrEmpty(payError))
                            {
                                model.PayResultList.Add("失败：" + payError);
                            }
                        }
                    }
                    else
                    {
                        //...
                    }

                    return validLog;
                });

                #endregion
            }
            #endregion
        }

        /// <summary>
        /// 获取支付相关数据
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="modelList"></param>
        /// <returns></returns>
        private void UpdateOrderPayDataByFile(string filePath, List<CheckoutOrder> modelList)
        {
            string lineText = null;
            int linePosition = 0;
            Dictionary<string, CheckoutOrder> checkoutOrderDic = modelList.ToDictionary(m => m.CheckoutGuid);
            List<string> checkoutGuidList = checkoutOrderDic.Keys.ToList();
            CheckoutOrder model = null;
            string sessionID = null;

            Dictionary<string, List<string>> sessionIDLogListDic = new Dictionary<string, List<string>>(0);
            using (StreamReader streamReader = new StreamReader(filePath, Encoding.UTF8))
            {
                while ((lineText = streamReader.ReadLine()) != null)
                {
                    linePosition++;
                    lineText = lineText.Replace("\\", "");

                    this.Logger.LogInformation($"正在读取第{linePosition}行数据...");

                    #region 2-获取弃单ID和会话ID关系
                    foreach (string checkoutGuid in checkoutGuidList)
                    {
                        if (lineText.Contains(checkoutGuid))
                        {
                            sessionID = null;
                            model = checkoutOrderDic[checkoutGuid];
                            if (lineText.Contains("调度", StringComparison.OrdinalIgnoreCase)
                                && lineText.Contains("token", StringComparison.OrdinalIgnoreCase))
                            {
                                sessionID = new Regex("(?<=\"token\":\")[^\"]+(?=\")").Match(lineText).Value;
                                if (!string.IsNullOrEmpty(sessionID) && !model.SessionIDList.Contains(sessionID))
                                {
                                    this.Logger.LogInformation($"第{linePosition}行数据读取到会话ID：Key={model.CheckoutGuid},SessionID={sessionID}");
                                    model.SessionIDList.Add(sessionID);
                                }
                            }
                        }
                    }
                    #endregion

                    #region 收集调度版本日志_后续分析使用

                    //调度版本
                    if (lineText.Contains("SessionID"))
                    {
                        sessionID = new Regex("(?<=\"SessionID\":\")[^\"]+(?=\")").Match(lineText).Value;

                        this.Logger.LogInformation($"第{linePosition}行数据已收集会话支付日志：SessionID={sessionID}");

                        if (sessionIDLogListDic.ContainsKey(sessionID))
                        {
                            sessionIDLogListDic[sessionID].Add(lineText);
                        }
                        else
                        {
                            sessionIDLogListDic.Add(sessionID, new List<string> { lineText });
                        }
                    }

                    #endregion

                }
            }

            int sessionIDPosition = 0;
            foreach (var sessionIDLogListItem in sessionIDLogListDic)
            {
                sessionIDPosition++;
                sessionID = sessionIDLogListItem.Key;
                this.Logger.LogInformation($"正在解析第{sessionIDPosition}/{sessionIDLogListDic.Count}会话支付日志...");
                model = modelList.FirstOrDefault(m => m.SessionIDList.Contains(sessionID));
                if (model != null)
                {
                    foreach (string sessionIDLog in sessionIDLogListItem.Value)
                    {
                        #region 获取结果和原因
                        string payType = null, payError = null;
                        if (sessionIDLog.Contains("CreateOrder_Result", StringComparison.OrdinalIgnoreCase))
                        {
                            //获取创建订单结果日志
                            if (!model.ESCreateOrderResultLogList.Contains(sessionIDLog))
                            {
                                model.ESCreateOrderResultLogList.Add(sessionIDLog);
                            }

                            if (sessionIDLog.Contains("PP_1002_CreateOrder_Result"))
                            {
                                payType = "PayPal";
                                payError = new Regex("(?<=\"issue\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                                if (string.IsNullOrEmpty(payError))
                                {
                                    payError = "成功";
                                }
                                else
                                {
                                    payError = "失败：" + payError;
                                }
                            }
                            else if (sessionIDLog.Contains("PayEaseDirect_1002_CreateOrder_Result"))
                            {
                                payType = "PayEaseDirect";
                                payError = new Regex("(?<=\"orderInfo\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                                if (string.IsNullOrEmpty(payError))
                                {
                                    payError = "成功";
                                }
                                else
                                {
                                    payError = "失败：" + payError;
                                }
                            }

                            if (!string.IsNullOrEmpty(payType) && !model.ESPayTypeList.Contains(payType))
                            {
                                this.Logger.LogInformation($"已收集订单支付方式：SessionID={sessionID},payType={payType}");
                                model.ESPayTypeList.Add(payType);
                            }
                            if (!string.IsNullOrEmpty(payError) && !model.CreateOrderResultList.Contains(payError))
                            {
                                this.Logger.LogInformation($"已收集创建订单结果：SessionID={sessionID},payError={payError}");
                                model.CreateOrderResultList.Add(payError);
                            }
                        }
                        else
                        {
                            //获取支付结果日志
                            if (sessionIDLog.Contains("PP_4002_CaptureOrder_Result", StringComparison.OrdinalIgnoreCase))
                            {
                                payType = "PayPal";
                                //获取创建订单结果日志
                                model.ESPayResultLogList.Add(sessionIDLog);

                                payError = new Regex("(?<=\"issue\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                                if (string.IsNullOrEmpty(payError))
                                {
                                    payError = "成功";
                                }
                                else
                                {
                                    payError = "失败：" + payError;
                                }
                            }
                            else if (sessionIDLog.Contains("PayEaseDirect_v1Controller_ResultPage", StringComparison.OrdinalIgnoreCase))
                            {
                                payType = "PayEaseDirect";

                                //获取创建订单结果日志
                                model.ESPayResultLogList.Add(sessionIDLog);

                                payError = new Regex("(?<=\"orderInfo\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                                if (string.IsNullOrEmpty(payError))
                                {
                                    payError = "成功";
                                }
                                else
                                {
                                    payError = "失败：" + payError;
                                }
                            }

                            if (!string.IsNullOrEmpty(payType) && !model.ESPayTypeList.Contains(payType))
                            {
                                this.Logger.LogInformation($"已收集订单支付方式：SessionID={sessionID},payType={payType}");
                                model.ESPayTypeList.Add(payType);
                            }
                            if (!string.IsNullOrEmpty(payError) && !model.PayResultList.Contains(payError))
                            {
                                this.Logger.LogInformation($"已收集订单支付结果：SessionID={sessionID},payError={payError}");
                                model.PayResultList.Add(payError);
                            }

                        }
                        #endregion
                    }
                }
            }

        }
    }
}
