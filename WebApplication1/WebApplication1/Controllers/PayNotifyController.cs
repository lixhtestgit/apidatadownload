using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebApplication1.Extension;
using WebApplication1.Helper;
using WebApplication1.Model;
using WebApplication1.Model.PayNotify;

namespace WebApplication1.Controllers
{
    [Route("api/PayNotify")]
    [ApiController]
    public class PayNotifyController : ControllerBase
    {
        protected HttpClient PayHttpClient;
        public ExcelHelper ExcelHelper;
        public IWebHostEnvironment WebHostEnvironment;
        public ILogger Logger;
        public IConfiguration Configuration;
        public ESSearchHelper ESSearchHelper;
        public MeShopHelper MeShopHelper;

        public PayNotifyController(
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            ILogger<PayNotifyController> logger,
            IConfiguration configuration,
            ESSearchHelper eSSearchHelper,
            MeShopHelper meShopHelper)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
            this.Logger = logger;
            this.Configuration = configuration;
            this.ESSearchHelper = eSSearchHelper;
            this.MeShopHelper = meShopHelper;
        }

        /// <summary>
        /// ES搜索订单支付方式推送
        /// api/PayNotify
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task<IActionResult> ESSearchOrderPayType()
        {
            string dataFilter = @"[
    {
        ""multi_match"": {
            ""type"": ""phrase"",
            ""query"": ""MeShopPay收到转发支付结果请求"",
            ""lenient"": true
        }
    },
    {
        ""bool"": {
            ""filter"": [
                {
                    ""multi_match"": {
                        ""type"": ""phrase"",
                        ""query"": ""PayResultV1SendRequestHelper"",
                        ""lenient"": true
                    }
                },
                {
                    ""multi_match"": {
                        ""type"": ""phrase"",
                        ""query"": ""WorldPayDirect"",
                        ""lenient"": true
                    }
                }
            ]
        }
    }
]";

            DateTime utcBeginDate = Convert.ToDateTime("2024-01-03 12:40:00");
            DateTime utcEndDate = Convert.ToDateTime("2024-01-03 12:40:00");

            List<ESLog> esLogList = await this.ESSearchHelper.GetESLogList($"WP直连", "martstores.com", dataFilter, utcBeginDate, utcEndDate, log =>
             {
                 return "1";
             });

            int totalCount = esLogList.Count;
            this.Logger.LogInformation($"获取到同步失败数据共{totalCount}个");
            string sessionID, notifyUrl;
            Regex sessionIDRegex = new Regex("(?<=\"token\":\")[a-z0-9]+(?=\")");
            Regex notifyUrlRegex = new Regex("(?<=\"NotifyUrl\":\")[^\"]+(?=\")");

            int position = 1;
            bool isSend = false;
            foreach (ESLog log in esLogList)
            {
                sessionID = sessionIDRegex.Match(log.Log).Value;
                notifyUrl = notifyUrlRegex.Match(log.Log).Value;
                if (notifyUrl.Contains("?"))
                {
                    notifyUrl += "&";
                }
                else
                {
                    notifyUrl += "?";
                }
                notifyUrl += "sessionID=" + sessionID;

                //notifyUrl = "http://localhost:8001/Callback/WorldPayDirect/notification";

                isSend = false;
                do
                {
                    var postResult = (HttpStatusCode.OK, "");
                    if (true)
                    {
                        postResult = await this.PayHttpClient.PostJson(notifyUrl, null);
                    }
                    else
                    {
                        string raw = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE paymentService PUBLIC ""-//Worldpay//DTD Worldpay PaymentService v1//EN"" ""http://dtd.worldpay.com/paymentService_v1.dtd"">
<paymentService version=""1.4"" merchantCode=""ARCMEADYNUSD"">
  <notify>
    <orderStatusEvent orderCode=""{sessionID}"">
      <payment>
        <lastEvent>REFUSED</lastEvent>
      </payment>
    </orderStatusEvent>
  </notify>
</paymentService>";
                        postResult = await this.PayHttpClient.PostJson(notifyUrl, raw, HttpClientExtension.CONTENT_TYPE_XML);
                    }

                    isSend = postResult.Item1 == System.Net.HttpStatusCode.OK;
                    this.Logger.LogInformation($"正在同步{position}/{totalCount}个异步通知消息到网站:{postResult.Item1},detail:{{notifyUrl={notifyUrl}}}");
                } while (isSend == false);

                position++;
            }

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        /// <summary>
        /// 支付公司导出订单异步同步推送
        /// </summary>
        /// <returns></returns>
        [Route("PayCompanyOrderSync")]
        [HttpGet]
        public async Task<IActionResult> PayCompanyOrderSync()
        {
            string contentRootPath = this.WebHostEnvironment.ContentRootPath;
            string testFilePath = $@"{contentRootPath}\示例测试目录\支付公司导出订单\10.31-11.2payu订单.xlsx";
            List<PayCompanyOrder> orderList = this.ExcelHelper.ReadTitleDataList<PayCompanyOrder>(testFilePath, new ExcelFileDescription(0));

            int totalCount = orderList.Count;
            this.Logger.LogInformation($"获取到同步失败数据共{totalCount}个");
            string notifyUrl = null;
            int position = 0;
            bool isSync = false;
            string payCompany = "Unlimint";
            string payHost = "pay.meshopstore.com";
            int orderSyncCount = 0;
            List<string> syncFailedList = new List<string>(10);
            foreach (PayCompanyOrder order in orderList)
            {
                position++;
                orderSyncCount = 0;
                order.SessionID = order.SessionID.Replace("'", "");
                notifyUrl = $"https://{payHost}/Callback/{payCompany}/Notification/{order.SessionID}";
                do
                {
                    isSync = false;
                    try
                    {
                        if (order.SessionID.Length == 32)
                        {
                            if (payCompany == "Cashfree")
                            {
                                var postResult = await this.PayHttpClient.PostForm(notifyUrl, new Dictionary<string, string>
                                {
                                    {"orderId", order.SessionID},
                                    {"txStatus", "SUCCESS"}
                                }, null);
                                isSync = postResult.Item1 == System.Net.HttpStatusCode.OK;
                            }
                            else if (payCompany == "Paytm")
                            {
                                var postResult = await this.PayHttpClient.PostForm(notifyUrl, new Dictionary<string, string>
                                {
                                    {"ORDERID", order.SessionID}
                                }, null);
                                isSync = postResult.Item1 == System.Net.HttpStatusCode.OK;
                            }
                            else if (payCompany == "Xendit")
                            {
                                var postResult = await this.PayHttpClient.PostJson(notifyUrl, JsonConvert.SerializeObject(new
                                {
                                    id = order.XenditInvoiceID,
                                    external_id = order.SessionID,
                                    status = "SETTLED"
                                }), null);
                                isSync = postResult.Item1 == System.Net.HttpStatusCode.OK;
                            }
                            else if (payCompany == "PayU")
                            {
                                notifyUrl = $"https://{payHost}/{payCompany}/v3/SyncSimpleOrder";

                                var postResult = await this.PayHttpClient.PostForm(notifyUrl, new Dictionary<string, string>
                                {
                                    { "sid" , order.SessionID},
                                    {"_token","2EZ539WKH7jFfJjVxfC7kMyQsc87xB9TwQeiuQAPQnMobcUxeC5yfXLGmbnnq9m4Vgmn5xgwLyUkJS29rWAYP3uZbc77kBD6ZSWA" }
                                }, null);
                                isSync = (postResult.Item1 == System.Net.HttpStatusCode.OK && postResult.Item2 == "Finish!");
                            }
                            else if (payCompany == "Unlimint")
                            {
                                var postResult = await this.PayHttpClient.PostJson(notifyUrl + "?admin_meshoppay", JsonConvert.SerializeObject(new
                                {
                                    merchant_order = new
                                    {
                                        id = "195d245d0c674025a2ebb84afd5fe281"
                                    },
                                    payment_data = new
                                    {
                                        id = "816831981",
                                        status = "DECLINED",
                                        decline_reason = "Declined by 3-D Secure"
                                    }
                                }), null);
                                isSync = (postResult.Item1 == System.Net.HttpStatusCode.OK && postResult.Item2 == "Finish!");
                            }
                        }
                    }
                    catch
                    {
                    }
                    orderSyncCount++;
                } while (isSync == false && orderSyncCount <= 3);
                if (isSync == false)
                {
                    syncFailedList.Add(order.SessionID);
                }
                this.Logger.LogInformation($"正在同步{position}/{totalCount}个异步通知消息到网站:{isSync},detail:{{notifyUrl={notifyUrl}}}");
            }

            if (syncFailedList.Count > 0)
            {
                this.Logger.LogError($"同步失败会话列表：{JsonConvert.SerializeObject(syncFailedList)}");
            }

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        /// <summary>
        /// 店铺弃单导出订单异步同步推送
        /// api/PayNotify/SyncPayFailedResult
        /// </summary>
        /// <returns></returns>
        [Route("SyncPayFailedResult")]
        [HttpGet]
        public async Task<IActionResult> SyncPayFailedResult()
        {
            string hostAdmin = "ericdressfashion";
            string contentRootPath = this.WebHostEnvironment.ContentRootPath;
            string testFilePath = $@"{contentRootPath}\示例测试目录\弃单\待刷失败原因弃单_收集数据20230228151051.xlsx";
            List<CheckoutOrder> checkoutOrderList = this.ExcelHelper.ReadTitleDataList<CheckoutOrder>(testFilePath, new ExcelFileDescription(0));

            int totalCount = checkoutOrderList.Count;
            this.Logger.LogInformation($"获取到同步失败数据共{totalCount}个");
            int position = 0;

            List<string> syncFailedList = new List<string>(10);
            foreach (CheckoutOrder checkoutOrder in checkoutOrderList)
            {
                position++;
                string payType = "Unlimint";
                string errorReason = checkoutOrder.PayResultList.LastOrDefault().Replace("失败：", "").Replace("'", "''");
                int execResult = await this.MeShopHelper.ExecSqlToShop(hostAdmin, $"update track_system set DataJSON='{{\"OrderErrorReason\":\"{errorReason}\",\"PayChannelName\":\"{payType}\"}}' where Type=2033 and TypeValueID={checkoutOrder.OrderID}");
                if (execResult <= 0)
                {
                    syncFailedList.Add(checkoutOrder.CheckoutGuid);
                }
                this.Logger.LogInformation($"正在同步{position}/{totalCount}个异步通知消息到网站:{execResult},checkoutGuid:{checkoutOrder.CheckoutGuid}");
            }

            if (syncFailedList.Count > 0)
            {
                this.Logger.LogError($"同步失败弃单列表：{JsonConvert.SerializeObject(syncFailedList)}");
            }

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        #region 扩展方法

        /// <summary>
        /// 国家字典（Key=两位编码，Value=名称）
        /// </summary>
        public Dictionary<string, string> CountryTwoCodeDic = new Dictionary<string, string>(300);
        /// <summary>
        /// 国家字典（Key=三位编码，Value=名称）
        /// </summary>
        public Dictionary<string, string> CountryThreeCodeDic = new Dictionary<string, string>(300);

        /// <summary>
        /// 根据国家编码获取国家名称
        /// </summary>
        /// <param name="countryCode"></param>
        /// <returns></returns>
        public async Task<string> GetCountryName(string countryCode)
        {
            string countryName = string.Empty;
            if (this.CountryTwoCodeDic.Count == 0)
            {
                var getResult = await this.PayHttpClient.Get("https://config.runshopstore.com/api/Geography/GetGeographyAll");

                JToken[] countryArray = JObject.Parse(getResult.Item2).SelectToken("data").ToArray();
                foreach (var item in countryArray)
                {
                    if (item.SelectToken("parent_code").ToString().IsNullOrEmpty())
                    {
                        string code = item.SelectToken("code").ToString();
                        string code3 = item.SelectToken("code3")?.ToString();
                        if (code.IsNotNullOrEmpty())
                        {
                            this.CountryTwoCodeDic.Add(code, item.SelectToken("name").ToString());
                        }
                        if (code3.IsNotNullOrEmpty())
                        {
                            this.CountryThreeCodeDic.Add(code3, item.SelectToken("name").ToString());
                        }
                    }
                }
            }
            if (this.CountryTwoCodeDic.ContainsKey(countryCode))
            {
                countryName = this.CountryTwoCodeDic[countryCode];
            }
            else if (this.CountryThreeCodeDic.ContainsKey(countryCode))
            {
                countryName = this.CountryThreeCodeDic[countryCode];
            }
            return countryName;
        }

        /// <summary>
        /// 获取店铺订单信息
        /// </summary>
        /// <param name="log">订单会话日志</param>
        /// <param name="sessionID">会话ID(获取不到填null)</param>
        /// <param name="tx">交易号(获取不到填null)</param>
        /// <param name="isUTCDate">是否是UTC时间</param>
        /// <returns></returns>
        public async Task<ShopOrder> GetShopOrder(string log, string sessionID, string tx, bool isUTCDate)
        {
            Regex sessionIDRegex = new Regex("(?<=\"(token|SessionID)\":\")[^\"]+(?=\")", RegexOptions.IgnoreCase);
            Regex siteCodeRegex = new Regex("(?<=\"siteCode\":\")[^\"]+(?=\")", RegexOptions.IgnoreCase);
            Regex orderCodeRegex = new Regex("(?<=\"(OrderCode|MerchantOrderID)\":\")[^\"]+(?=\")", RegexOptions.IgnoreCase);
            Regex totalPayPriceRegex = new Regex("(?<=\"TotalPayPrice\":)[^}]+(?=})", RegexOptions.IgnoreCase);
            Regex txRegex = new Regex("(?<=\"tx\":\")[^\"]+(?=\")", RegexOptions.IgnoreCase);
            Regex emailRegex = new Regex("(?<=\"Email\":\")[^\"]+(?=\")", RegexOptions.IgnoreCase);
            Regex phoneRegex = new Regex("(?<=\"Phone\":\")[^\"]+(?=\")", RegexOptions.IgnoreCase);
            Regex firstNameRegex = new Regex("(?<=\"FirstName\":\")[^\"]+(?=\")", RegexOptions.IgnoreCase);
            Regex lastNameRegex = new Regex("(?<=\"LastName\":\")[^\"]+(?=\")", RegexOptions.IgnoreCase);
            Regex countryRegex = new Regex("(?<=\"CountryCode\":\")[^\"]+(?=\")", RegexOptions.IgnoreCase);
            Regex provinceRegex = new Regex("(?<=\"Province\":\")[^\"]+(?=\")", RegexOptions.IgnoreCase);
            Regex cityRegex = new Regex("(?<=\"City\":\")[^\"]+(?=\")", RegexOptions.IgnoreCase);
            Regex addressRegex = new Regex("(?<=\"Address1\":\")[^\"]+(?=\")", RegexOptions.IgnoreCase);
            Regex zipRegex = new Regex("(?<=\"ZIP\":\")[^\"]+(?=\")", RegexOptions.IgnoreCase);

            ShopOrder shopOrder = null;
            if (log.IsNotNullOrEmpty())
            {
                shopOrder = new ShopOrder
                {
                    SessionID = sessionID.IsNotNullOrEmpty() ? sessionID : sessionIDRegex.Match(log)?.Value,
                    SiteCode = siteCodeRegex.Match(log)?.Value,
                    OrderCode = orderCodeRegex.Match(log)?.Value,
                    TotalPayPrice = totalPayPriceRegex.Match(log)?.Value,
                    CreateTime = log.Length > 9 ? log.Substring(0, 19) : "",
                    TX = tx.IsNotNullOrEmpty() ? tx : txRegex.Match(log)?.Value,
                    Product = null,
                    Email = emailRegex.Match(log)?.Value,
                    Phone = phoneRegex.Match(log)?.Value,
                    FirstName = firstNameRegex.Match(log)?.Value,
                    LastName = lastNameRegex.Match(log)?.Value,
                    Country = countryRegex.Match(log)?.Value,
                    Province = provinceRegex.Match(log)?.Value,
                    City = cityRegex.Match(log)?.Value,
                    Address = addressRegex.Match(log)?.Value,
                    Zip = zipRegex.Match(log)?.Value
                };

                if (isUTCDate)
                {
                    shopOrder.CreateTime = Convert.ToDateTime(shopOrder.CreateTime).AddHours(8).ToString_yyyyMMddHHmmss();
                }

                shopOrder.Country = await this.GetCountryName(shopOrder.Country);

                //获取产品信息
                if (shopOrder.OrderCode.IsNotNullOrEmpty())
                {
                    string dataFilter = $@"[
                    {{
                        ""multi_match"": {{
                            ""type"": ""phrase"",
                            ""query"": ""{shopOrder.OrderCode.Split(',')[1]}"",
                            ""lenient"": true
                        }}
                    }},
                    {{
                        ""multi_match"": {{
                            ""type"": ""phrase"",
                            ""query"": ""/product/"",
                            ""lenient"": true
                        }}
                    }}
                ]";

                    int logDays = (int)(DateTime.Now - shopOrder.ShowCreateTime.AddHours(8)).TotalDays + 3;

                    DateTime utcBeginDate = DateTime.UtcNow.AddDays(logDays * -1);
                    DateTime utcEndDate = DateTime.UtcNow;

                    List<ESLog> esLogList = await this.ESSearchHelper.GetESLogList($"获取调度数据", "meshopstore.com", dataFilter, utcBeginDate, utcEndDate, log =>
                    {
                        return "1";
                    });
                    if (esLogList.Count > 0)
                    {
                        Regex productUrlRegex = new Regex("(?<=\")https://[^\"]+(?=\")", RegexOptions.IgnoreCase);
                        shopOrder.Product = productUrlRegex.Match(esLogList[0].Log)?.Value;
                    }
                }
            }
            return shopOrder;
        }

        #endregion

    }
}
