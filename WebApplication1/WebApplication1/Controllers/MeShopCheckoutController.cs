using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    [Route("api/MeShopCheckout")]
    [ApiController]
    public class MeShopCheckoutController : ControllerBase
    {
        protected HttpClient PayHttpClient { get; set; }
        public ExcelHelper ExcelHelper { get; set; }
        public ILogger Logger { get; set; }
        public ESSearchHelper ESSearchHelper { get; set; }
        public CheckoutBIZ CheckoutBIZ { get; set; }

        public MeShopCheckoutController(
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
        /// ES搜索弃单支付方式和订单创建，支付相关日志
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task<IActionResult> ESSearchOrderPayType()
        {
            string filePath = $@"C:\Users\lixianghong\Desktop\弃单数据_{DateTime.Now.ToString("yyyyMMdd")}.xlsx";

            string[] checkoutGuidArray = new string[] {
                "0249bea1-d38c-4d40-a30f-29f34972e64a",
                "0c19751e-2a65-4346-9e2b-8decca3fecf2",
                "1d2ffec2-cd90-4cb4-b4ec-4b2cc2bbdf3f",
                "53d0d2c2-2762-48b1-ba2b-0a76328838a9",
                "6db00c5e-8d10-414c-8651-c2708578e985",
                "78f6016f-bca5-47ff-bfc1-030a49fda307",
                "eff3a06c-032a-4b6b-9f5e-dab28f5fc060",
                "f53d2c6b-c117-4c93-873f-3a3c81ed8147",
                "fd7c50eb-b197-410b-956a-cb775c20091f"
            };
            List<CheckoutOrder> dataList = await this.GetESCheckoutOrderList(checkoutGuidArray);

            IWorkbook workbook = ExcelHelper.CreateOrUpdateWorkbook(dataList);
            ExcelHelper.SaveWorkbookToFile(workbook, filePath);

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        /// <summary>
        /// ES搜索WP异步通知
        /// </summary>
        /// <returns></returns>
        [Route("ESWP")]
        [HttpGet]
        public async Task<IActionResult> ESWP()
        {
            string dataFilter = @"[
    {
        ""multi_match"": {
            ""type"": ""phrase"",
            ""query"": ""soundo-shop"",
            ""lenient"": true
        }
    },
    {
        ""multi_match"": {
            ""type"": ""phrase"",
            ""query"": ""1_收到异步通知"",
            ""lenient"": true
        }
    }
]";

            List<string> validList = new List<string>(100);

            List<ESLog> esLogList = await this.ESSearchHelper.GetESLogList($"WP日志", "meshopstore.com", dataFilter, 2, log =>
            {
                string payType = null;
                if (log.Contains("worldpay", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "worldpay";

                    log = log.Replace("\\\"", "\"");
                    string logDate = Convert.ToDateTime(log.Substring(0, 19)).AddHours(8).ToString("yyyy-MM-dd HH:mm:ss");
                    string orderCode = new Regex("(?<=orderCode=\")[^\"]+(?=\")").Match(log).Value;
                    string lastEvent = new Regex("(?<=<lastEvent>)[^<]+(?=<)").Match(log).Value;
                    string returnCode = new Regex("(?<=ISO8583ReturnCode.+description=\")[^\"]+(?=\")").Match(log).Value;
                    validList.Add($"{orderCode}_{logDate}_{lastEvent}_{returnCode}");
                }
                return payType;
            });
            validList.Sort();
            string wpValidLogStr = string.Join("\r\n", validList);

            return Ok();
        }

        /// <summary>
        /// 获取ES弃单订单列表
        /// </summary>
        /// <param name="checkoutGuidArray"></param>
        /// <returns></returns>
        public async Task<List<CheckoutOrder>> GetESCheckoutOrderList(params string[] checkoutGuidArray)
        {
            List<CheckoutOrder> checkoutOrderList = new List<CheckoutOrder>(checkoutGuidArray.Length);

            //通过ES更新查询支付方式
            int position = 1;
            int totalCount = checkoutGuidArray.Length;
            int days = 14;
            foreach (string checkoutGuid in checkoutGuidArray)
            {
                CheckoutOrder checkoutOrder = new CheckoutOrder
                {
                    CheckoutGuid = checkoutGuid
                };

                //查询支付方式
                await this.UpdateOrderPayDataByES(totalCount, position, checkoutOrder, days);

                checkoutOrderList.Add(checkoutOrder);
                position++;
            }

            return checkoutOrderList;
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
        private async Task UpdateOrderPayDataByES(int totalCount, int position, CheckoutOrder model, int lastDays)
        {
            if (model.ESPayTypeList == null)
            {
                model.ESPayTypeList = new List<string>(1);
            }
            if (model.SessionIDList == null)
            {
                model.SessionIDList = new List<string>(1);
            }
            if (model.CreateOrderResultList == null)
            {
                model.CreateOrderResultList = new List<string>(1);
            }
            if (model.PayResultList == null)
            {
                model.PayResultList = new List<string>(1);
            }
            if (model.ESCreateOrderResultLogList == null)
            {
                model.ESCreateOrderResultLogList = new List<string>(1);
            }
            if (model.ESPayResultLogList == null)
            {
                model.ESPayResultLogList = new List<string>(1);
            }

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
            Regex payTypeRegex = new Regex("(?<=ajax/(paydd|pay)/)[^(\\?|\\s)]+(?=(\\?|\\s))", RegexOptions.IgnoreCase);

            List<ESLog> esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个支付类型数据", "meshopstore.com", dataFilter, lastDays, log =>
            {
                string payType = payTypeRegex.Match(log)?.Value;

                return payType;
            });

            if (esLogList.Count > 0)
            {
                model.ESPayTypeList.AddRange(esLogList.Select(m => m.Type));
            }

            #endregion

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
            esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个SessionID数据", "meshopstore.com", dataFilter, lastDays, log =>
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

                esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个弃单会话结果数据", "meshopstore.com", dataFilter, lastDays, log =>
                {
                    string validLog = null;
                    string resultRemark = null;
                    if (log.Contains("CreateOrder_Result", StringComparison.OrdinalIgnoreCase))
                    {
                        //获取创建订单结果日志
                        validLog = log;
                        model.ESCreateOrderResultLogList.Add(validLog);

                        if (model.ESPayType.Contains("PayPal"))
                        {
                            resultRemark = new Regex("(?<=\"issue\":\")[^\"]+(?=\")").Match(log).Value;
                        }
                        else if (model.ESPayType.Contains("Paytm") && !validLog.Contains("Success"))
                        {
                            resultRemark = new Regex("(?<=\"resultMsg\":\")[^\"]+(?=\")").Match(log).Value;
                        }
                        if (!string.IsNullOrEmpty(resultRemark))
                        {
                            model.CreateOrderResultList.Add("失败：" + resultRemark);
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

                            resultRemark = new Regex("(?<=\"issue\":\")[^\"]+(?=\")").Match(log).Value;
                            resultRemark = $"失败：{resultRemark}";
                        }
                        else if (model.ESPayType.Contains("PayEase_Direct")
                            && log.Contains("PayEaseDirect_v1Controller_ResultPage", StringComparison.OrdinalIgnoreCase))
                        {
                            //获取创建订单结果日志
                            validLog = log;
                            model.ESPayResultLogList.Add(validLog);

                            resultRemark = new Regex("(?<=\"orderInfo\":\")[^\"]+(?=\")").Match(log).Value;
                            resultRemark = $"失败：{resultRemark}";
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
                                resultRemark = new Regex("(?<=\"resultMsg\":\")[^\"]+(?=\")").Match(log).Value;
                                resultRemark = $"失败：{resultRemark}";
                            }
                        }
                        else if (model.ESPayType.Contains("Unlimint")
                            && log.Contains("MeShopPay,收到异步通知", StringComparison.OrdinalIgnoreCase))
                        {
                            //获取创建订单结果日志
                            validLog = log;
                            model.ESPayResultLogList.Add(validLog);

                            string unlimintState = new Regex("(?<=\"status\":\")[^\"]+(?=\")").Match(log).Value;
                            resultRemark = new Regex("(?<=\"decline_reason\":\")[^\"]+(?=\")").Match(log).Value;

                            if (unlimintState.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
                            {
                                resultRemark = "成功";
                            }
                            else
                            {
                                resultRemark = $"失败：{unlimintState}:{resultRemark}";
                            }

                        }

                        if (!string.IsNullOrEmpty(resultRemark))
                        {
                            model.PayResultList.Add(resultRemark);
                        }
                    }
                    return validLog;
                });

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
