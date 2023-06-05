using Microsoft.AspNetCore.Hosting;
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
using WebApplication1.Extension;
using WebApplication1.Helper;
using WebApplication1.Model;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 获取店铺弃单数据并获取对应支付方式和失败原因日志
    /// </summary>
    [Route("api/MeShopCheckout")]
    [ApiController]
    public class MeShopCheckoutController : ControllerBase
    {
        public IWebHostEnvironment WebHostEnvironment;
        protected HttpClient PayHttpClient;
        public ExcelHelper ExcelHelper;
        public ILogger Logger;
        public ESSearchHelper ESSearchHelper;
        public CheckoutBIZ CheckoutBIZ;

        public MeShopCheckoutController(
            IWebHostEnvironment webHostEnvironment,
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            ILogger<MeShopCheckoutController> logger,
            ESSearchHelper eSSearchHelper,
            CheckoutBIZ checkoutBIZ)
        {
            this.WebHostEnvironment = webHostEnvironment;
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.Logger = logger;
            this.ESSearchHelper = eSSearchHelper;
            this.CheckoutBIZ = checkoutBIZ;
        }

        /// <summary>
        /// ES搜索弃单支付方式和订单创建，支付失败等相关日志
        /// api/MeShopCheckout/
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task<IActionResult> ESSearchOrderByCheckout()
        {
            #region 收集数据源1：通过手动收集弃单集合
            //string[] checkoutGuidArray = new string[] {
            //    "036135ee-5174-47b1-9aa4-5dd2f7c9d303","059684cd-9ea8-4239-8b46-c3d31d7e89b9","2434fe89-9f40-4cb5-8c0f-8a1ca9460983","57132ecc-1308-48ae-83f3-305da3690951","79f9ea18-e0e9-4538-9649-d6cde3556266","7fe01ce5-fce8-4d8d-a230-406511f34fe3","a2ae7972-947d-4f4c-a539-3a5d7b70efb7","adb4fc45-08df-4b6b-85c2-45b189269174","b7aee623-6796-4764-a131-913dbaad9575","bd7d3dc1-8f6b-4b52-855c-f31445fe8217","c0198dc4-0edc-43a0-9765-4dbddfd5b45a","c5a4431e-86a7-4f4e-82da-1013f2acf41a","efbfdb54-50bd-42f3-a22e-0734c9da1ece","fa0d410a-a7fe-4161-9640-0d379cbfb36d"
            //};
            //List<CheckoutOrder> dataList = await this.GetESCheckoutOrderList(checkoutGuidArray);
            #endregion

            #region 收集数据源2：通过API收集弃单集合
            string shopDomain = "teamliu5.meshopstore.com";
            string email = "sellershop@126.com";
            string pwd = "12Meoslp7238nbv";
            DateTime beginDate = Convert.ToDateTime("2023-06-04 10:00:00");
            DateTime endDate = Convert.ToDateTime("2023-06-04 16:00:00");
            List<CheckoutOrder> dataList = await this.CheckoutBIZ.GetList(shopDomain, email, pwd, beginDate, endDate, new int[] { -1, 0, 1 });
            #endregion

            #region 收集数据源3：通过弃单导出文件收集弃单集合

            //string testFilePath = $@"{contentRootPath}\示例测试目录\弃单\待刷失败原因弃单.xlsx";
            //List<CheckoutOrder> dataList = this.ExcelHelper.ReadTitleDataList<CheckoutOrder>(testFilePath, new ExcelFileDescription(0));

            #endregion

            #region 分析失败原因1：通过弃单导出文件收集弃单集合

            //通过ES分析失败原因
            await this.GetESCheckoutOrderList(dataList);

            #endregion

            #region 分析失败原因2：通过支付日志文件分析失败原因

            //通过支付日志文件分析失败原因
            //string payLogFilePath = $@"{contentRootPath}\示例测试目录\支付公司导出订单\pacyhost.log";
            //this.UpdateOrderPayDataByFile(payLogFilePath, dataList);
            #endregion

            string contentRootPath = this.WebHostEnvironment.ContentRootPath;
            string filePath = $@"{contentRootPath}\示例测试目录\弃单\待刷失败原因弃单_收集数据{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
            IWorkbook workbook = ExcelHelper.CreateOrUpdateWorkbook(dataList);
            ExcelHelper.SaveWorkbookToFile(workbook, filePath);

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        /// <summary>
		/// ES获取店铺域名URL请求分类统计
		/// api/MeShopCheckout/test
		/// </summary>
		/// <returns></returns>
		[Route("test")]
        [HttpGet]
        public async Task<IActionResult> Test()
        {
            string dataFilter = @"[
            {
                ""multi_match"": {
                    ""type"": ""best_fields"",
                    ""query"": ""cbeht.top"",
                    ""lenient"": true
                }
            },
            {
                ""multi_match"": {
                    ""type"": ""phrase"",
                    ""query"": ""meshop-8c2d5e72-meshop-shop-5555"",
                    ""lenient"": true
                }
            }
        ]";

            List<ESLog> esLogList = await this.ESSearchHelper.GetESNginxLogList($"xxx", "meshopstore.com", dataFilter, 1, log =>
            {
                return "1";
            });

            Dictionary<string, int> nginxPageUrlCountDic = new Dictionary<string, int>(1000);

            Regex payUrlRegex = new Regex("(?<=\"(POST|GET){1}\\s+)[^\\s]+(?=\\s+)", RegexOptions.IgnoreCase);

            foreach (ESLog item in esLogList)
            {
                string pagUrl = payUrlRegex.Match(item.Log).Value;
                if (pagUrl.IsNotNullOrEmpty())
                {
                    if (nginxPageUrlCountDic.ContainsKey(pagUrl))
                    {
                        nginxPageUrlCountDic[pagUrl]++;
                    }
                    else
                    {
                        nginxPageUrlCountDic.Add(pagUrl, 1);
                    }
                }
            }
            var showSortArray = nginxPageUrlCountDic.OrderByDescending(m => m.Value).ToArray();

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        /// <summary>
        /// 获取ES弃单订单列表
        /// </summary>
        /// <param name="checkoutGuidArray"></param>
        /// <returns></returns>
        private async Task GetESCheckoutOrderList(List<CheckoutOrder> checkoutOrderList)
        {
            //通过ES更新查询支付方式
            int position = 1;
            int totalCount = checkoutOrderList.Count;
            int days = 14;
            foreach (CheckoutOrder checkout in checkoutOrderList)
            {
                if (checkout.CheckoutGuid.IsNotNullOrEmpty())
                {
                    //查询支付方式
                    await this.UpdateOrderPayDataByES(totalCount, position, checkout, days);
                }

                position++;
            }
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
            #region 1-获取会话ID

            string dataFilter = @"[
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
            await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个SessionID数据", "meshopstore.com", dataFilter, lastDays, log =>
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

            #region 2-获取回话结果日志

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

                await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个弃单会话结果数据", "meshopstore.com", dataFilter, lastDays, sessionIDLog =>
                {
                    //获取结果和原因
                    this.UpdatePayTypeAndCreateOrderResult(model, sessionIDLog);
                    this.UpdatePayTypeAndPayResult(model, sessionIDLog);
                    this.UpdatePayAccount(model, sessionIDLog);

                    return "1";
                });

            }

            #endregion

        }

        /// <summary>
        /// 获取支付相关数据
        /// </summary>
        /// <param name="filePath">支付日志文件</param>
        /// <param name="modelList">弃单列表</param>
        /// <returns></returns>
        private void UpdateOrderPayDataByFile(string filePath, List<CheckoutOrder> modelList)
        {
            modelList.RemoveAll(m => m.CheckoutGuid.IsNullOrEmpty());

            string lineText = null;
            int linePosition = 0;
            Dictionary<string, CheckoutOrder> checkoutOrderDic = new Dictionary<string, CheckoutOrder>(modelList.Count);
            foreach (CheckoutOrder item in modelList)
            {
                if (!checkoutOrderDic.ContainsKey(item.CheckoutGuid))
                {
                    checkoutOrderDic.Add(item.CheckoutGuid, item);
                }
            }

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
                    sessionID = null;
                    if (lineText.Contains("SessionID"))
                    {
                        sessionID = new Regex("(?<=\"SessionID\":\")[^\"]+(?=\")").Match(lineText).Value;
                    }
                    else if (lineText.Contains("收到异步通知"))
                    {
                        //PacyPayHosted摘取有用日志
                        if (lineText.Contains("merchantTxnId"))
                        {
                            sessionID = new Regex("(?<=\"merchantTxnId\":\")[^\"]+(?=\")").Match(lineText).Value;
                        }
                    }

                    if (sessionID.IsNotNullOrEmpty())
                    {
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
                        //获取结果和原因
                        this.UpdatePayTypeAndCreateOrderResult(model, sessionIDLog);
                        this.UpdatePayTypeAndPayResult(model, sessionIDLog);
                    }
                }
            }

        }

        /// <summary>
        /// 更新收款账号
        /// </summary>
        /// <param name="checkoutOrder"></param>
        /// <param name="sessionIDLog"></param>
        private void UpdatePayAccount(CheckoutOrder checkoutOrder, string sessionIDLog)
        {
            string payAccountName = null;
            if (sessionIDLog.Contains("HomeController,Index,1_收到调度转发请求", StringComparison.OrdinalIgnoreCase))
            {
                payAccountName = new Regex("(?<=\"MerchantCode\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                if (!string.IsNullOrEmpty(payAccountName))
                {
                    checkoutOrder.ESPayAccountList.Add(payAccountName);
                }
            }
        }

        /// <summary>
        /// 更新支付方式和创建订单结果
        /// </summary>
        /// <param name="checkoutOrder"></param>
        /// <param name="sessionIDLog"></param>
        private void UpdatePayTypeAndCreateOrderResult(CheckoutOrder checkoutOrder, string sessionIDLog)
        {
            bool createOrderResult = false;
            string result = null;
            string payType = null;

            if (sessionIDLog.Contains("CreateOrder_Result", StringComparison.OrdinalIgnoreCase)
                            || sessionIDLog.Contains("CreateOrder_1002_Result", StringComparison.OrdinalIgnoreCase))
            {
                if (sessionIDLog.Contains("PP"))
                {
                    payType = "PayPal";
                    //获取日志
                    checkoutOrder.ESCreateOrderResultLogList.Add(sessionIDLog);

                    result = new Regex("(?<=\"issue\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                    if (string.IsNullOrEmpty(result))
                    {
                        createOrderResult = true;
                    }
                }
                else if (sessionIDLog.Contains("PayEaseDirect"))
                {
                    payType = "PayEaseDirect";
                    //获取日志
                    checkoutOrder.ESCreateOrderResultLogList.Add(sessionIDLog);

                    result = new Regex("(?<=\"orderInfo\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                    if (string.IsNullOrEmpty(result))
                    {
                        createOrderResult = true;
                    }
                }
                else if (sessionIDLog.Contains("PacyPayHosted"))
                {
                    payType = "PacyPayHosted";
                    //获取日志
                    checkoutOrder.ESCreateOrderResultLogList.Add(sessionIDLog);

                    result = new Regex("(?<=\"status\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                    if (result == "S")
                    {
                        createOrderResult = true;
                    }
                }
                else if (sessionIDLog.Contains("PacyPayDirect"))
                {
                    payType = "PacyPayDirect";
                    //获取日志
                    checkoutOrder.ESCreateOrderResultLogList.Add(sessionIDLog);

                    result = new Regex("(?<=\"status\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                    if (result == "S")
                    {
                        createOrderResult = true;
                    }
                }
                else if (sessionIDLog.Contains("Paytm"))
                {
                    payType = "Paytm";
                    //获取日志
                    checkoutOrder.ESCreateOrderResultLogList.Add(sessionIDLog);

                    result = new Regex("(?<=\"resultMsg\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                    if (result == "Success")
                    {
                        createOrderResult = true;
                    }
                }
                else if (sessionIDLog.Contains("Unlimint"))
                {
                    payType = "Unlimint";
                    //获取日志
                    checkoutOrder.ESCreateOrderResultLogList.Add(sessionIDLog);

                    result = new Regex("(?<=\"message\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                    if (result.IsNullOrEmpty())
                    {
                        createOrderResult = true;
                    }
                }

                if (payType.IsNotNullOrEmpty())
                {
                    if (createOrderResult)
                    {
                        result = "成功";
                    }
                    else
                    {
                        result = "失败：" + result;
                    }

                    if (!string.IsNullOrEmpty(payType) && !checkoutOrder.ESPayTypeList.Contains(payType))
                    {
                        this.Logger.LogInformation($"已收集订单支付方式：cartCheckoutGuid={checkoutOrder.CheckoutGuid},payType={payType}");
                        checkoutOrder.ESPayTypeList.Add(payType);
                    }
                    if (!string.IsNullOrEmpty(result) && !checkoutOrder.CreateOrderResultList.Contains(result))
                    {
                        this.Logger.LogInformation($"已收集创建订单结果：cartCheckoutGuid={checkoutOrder.CheckoutGuid},result={result}");
                        checkoutOrder.CreateOrderResultList.Add(result);
                    }
                }
            }
        }

        /// <summary>
        /// 更新支付方式和支付结果
        /// </summary>
        /// <param name="checkoutOrder"></param>
        /// <param name="sessionIDLog"></param>
        private void UpdatePayTypeAndPayResult(CheckoutOrder checkoutOrder, string sessionIDLog)
        {
            string payType = null;
            string result = null;
            bool payResult = false;

            //获取支付结果日志
            if (sessionIDLog.Contains("PP_4002_CaptureOrder_Result", StringComparison.OrdinalIgnoreCase))
            {
                payType = "PayPal";
                //获取日志
                checkoutOrder.ESPayResultLogList.Add(sessionIDLog);

                result = new Regex("(?<=\"issue\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                if (string.IsNullOrEmpty(result))
                {
                    payResult = true;
                }
            }
            else if (sessionIDLog.Contains("PayEaseDirect_v1Controller_ResultPage", StringComparison.OrdinalIgnoreCase))
            {
                payType = "PayEaseDirect";

                //获取创建订单结果日志
                checkoutOrder.ESPayResultLogList.Add(sessionIDLog);

                result = new Regex("(?<=\"orderInfo\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                if (string.IsNullOrEmpty(result))
                {
                    payResult = true;
                }
            }
            else if (sessionIDLog.Contains("Paytm_2002_GetOrder_Result", StringComparison.OrdinalIgnoreCase))
            {
                payType = "Paytm";

                //获取创建订单结果日志
                checkoutOrder.ESPayResultLogList.Add(sessionIDLog);

                result = new Regex("(?<=\"resultMsg\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;

                if (sessionIDLog.Contains("PENDING")
                    || sessionIDLog.Contains("TXN_SUCCESS"))
                {
                    payResult = true;
                }
            }
            else if (sessionIDLog.Contains("MeShopPay,收到异步通知", StringComparison.OrdinalIgnoreCase))
            {
                if (sessionIDLog.Contains("PacyPayHosted", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "PacyPayHosted";

                    //获取创建订单结果日志
                    checkoutOrder.ESPayResultLogList.Add(sessionIDLog);

                    string pacyPayHostedState = new Regex("(?<=\"status\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                    result = new Regex("(?<=\"respMsg\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;

                    if (pacyPayHostedState.Equals("S", StringComparison.OrdinalIgnoreCase))
                    {
                        payResult = true;
                    }
                    else
                    {
                        //如果respMsg获取不到数据，再从reason中获取
                        if (result.IsNullOrEmpty())
                        {
                            result = new Regex("(?<=\"reason\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                        }
                    }
                }
                else if (sessionIDLog.Contains("PacyPayDirect", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "PacyPayDirect";

                    //获取创建订单结果日志
                    checkoutOrder.ESPayResultLogList.Add(sessionIDLog);

                    string pacyPayDirectState = new Regex("(?<=\"status\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                    result = new Regex("(?<=\"respMsg\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;

                    if (pacyPayDirectState.Equals("S", StringComparison.OrdinalIgnoreCase))
                    {
                        payResult = true;
                    }
                    else
                    {
                        //如果respMsg获取不到数据，再从reason中获取
                        if (result.IsNullOrEmpty())
                        {
                            result = new Regex("(?<=\"reason\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                        }
                    }
                }
                else if (sessionIDLog.Contains("Unlimint", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "Unlimint";

                    //获取创建订单结果日志
                    checkoutOrder.ESPayResultLogList.Add(sessionIDLog);

                    string unlimintState = new Regex("(?<=\"status\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;
                    result = new Regex("(?<=\"decline_reason\":\")[^\"]+(?=\")").Match(sessionIDLog).Value;

                    if (unlimintState.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
                    {
                        payResult = true;
                    }
                }
            }

            if (payType.IsNotNullOrEmpty())
            {
                if (payResult)
                {
                    result = "成功";
                }
                else
                {
                    result = "失败：" + result;
                }

                if (!string.IsNullOrEmpty(payType) && !checkoutOrder.ESPayTypeList.Contains(payType))
                {
                    this.Logger.LogInformation($"已收集订单支付方式：cartCheckoutGuid={checkoutOrder.CheckoutGuid},payType={payType}");
                    checkoutOrder.ESPayTypeList.Add(payType);
                }
                if (!string.IsNullOrEmpty(result) && !checkoutOrder.PayResultList.Contains(result))
                {
                    this.Logger.LogInformation($"已收集订单支付结果：cartCheckoutGuid={checkoutOrder.CheckoutGuid},result={result}");
                    checkoutOrder.PayResultList.Add(result);
                }
            }
        }
    }
}
