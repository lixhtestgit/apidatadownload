using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
using WebApplication1.Extension;
using WebApplication1.Helper;
using WebApplication1.Model.MeShop;
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
            ILogger<TestController> logger,
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
            ""query"": ""PAYSUCCESS-INFO-PROCESS"",
            ""lenient"": true
        }
    },
    {
        ""bool"": {
            ""filter"": [
                {
                    ""multi_match"": {
                        ""type"": ""phrase"",
                        ""query"": ""PayPal"",
                        ""lenient"": true
                    }
                },
                {
                    ""bool"": {
                        ""should"": [
                            {
                                ""multi_match"": {
                                    ""type"": ""phrase"",
                                    ""query"": ""ericdress"",
                                    ""lenient"": true
                                }
                            },
                            {
                                ""multi_match"": {
                                    ""type"": ""phrase"",
                                    ""query"": ""tbdress"",
                                    ""lenient"": true
                                }
                            }
                        ],
                        ""minimum_should_match"": 1
                    }
                }
            ]
        }
    }
]";

            List<ESLog> esLogList = await this.ESSearchHelper.GetESLogList($"获取独立站支付成功数据", "meshopstore.com", dataFilter, 20, log =>
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
                isSend = false;
                do
                {
                    var postResult = await this.PayHttpClient.Post(notifyUrl, null);
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
            string payCompany = "PayU";
            string payHost = "pay.qffun.com";
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
                                var postResult = await this.PayHttpClient.Post(notifyUrl, new Dictionary<string, string>
                                {
                                    {"orderId", order.SessionID},
                                    {"txStatus", "SUCCESS"}
                                }, null);
                                isSync = postResult.Item1 == System.Net.HttpStatusCode.OK;
                            }
                            else if (payCompany == "Paytm")
                            {
                                var postResult = await this.PayHttpClient.Post(notifyUrl, new Dictionary<string, string>
                                {
                                    {"ORDERID", order.SessionID}
                                }, null);
                                isSync = postResult.Item1 == System.Net.HttpStatusCode.OK;
                            }
                            else if (payCompany == "Xendit")
                            {
                                var postResult = await this.PayHttpClient.Post(notifyUrl, JsonConvert.SerializeObject(new
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

                                var postResult = await this.PayHttpClient.Post(notifyUrl, new Dictionary<string, string>
                                {
                                    { "sid" , order.SessionID},
                                    {"_token","2EZ539WKH7jFfJjVxfC7kMyQsc87xB9TwQeiuQAPQnMobcUxeC5yfXLGmbnnq9m4Vgmn5xgwLyUkJS29rWAYP3uZbc77kBD6ZSWA" }
                                }, null);
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
        /// 生成PayU订单表格
        /// </summary>
        /// <returns></returns>
        [Route("BuildPayUOrderTable")]
        [HttpGet]
        public async Task<IActionResult> BuildPayUOrderTable()
        {
            List<ShopOrder> orderList = new List<ShopOrder>(500);

            string contentRootPath = this.WebHostEnvironment.ContentRootPath;
            string testFilePath = $@"{contentRootPath}\示例测试目录\支付公司导出订单\10.31-11.2payu订单.xlsx";
            List<PayCompanyOrder> payCompanyOrderList = this.ExcelHelper.ReadTitleDataList<PayCompanyOrder>(testFilePath, new ExcelFileDescription(0));

#if DEBUG
            //payCompanyOrderList.RemoveRange(1, payCompanyOrderList.Count - 1);
#endif
            int dataPosition = 0;
            int dataTotalCount = payCompanyOrderList.Count;
            foreach (PayCompanyOrder item in payCompanyOrderList)
            {
                dataPosition++;
                this.Logger.LogInformation($"正在处理第{dataPosition}/{dataTotalCount}个数据...");

                string dataFilter = $@"[
                    {{
                        ""multi_match"": {{
                            ""type"": ""phrase"",
                            ""query"": ""{item.SessionID}"",
                            ""lenient"": true
                        }}
                    }},
                    {{
                        ""multi_match"": {{
                            ""type"": ""phrase"",
                            ""query"": ""MeShop_DispatchPay,会话初始化页面,获取结果"",
                            ""lenient"": true
                        }}
                    }}
                ]";

                List<ESLog> esLogList = await this.ESSearchHelper.GetESLogList($"获取调度数据", "meshopstore.com", dataFilter, 6, log =>
                {
                    return "1";
                });
                if (esLogList.Count > 0)
                {
                    orderList.Add(await this.GetShopOrder(esLogList[0].Log, item.SessionID, item.TX, false));
                }
            }

            IWorkbook workBook = this.ExcelHelper.CreateOrUpdateWorkbook(orderList);
            this.ExcelHelper.SaveWorkbookToFile(workBook, @"C:\Users\lixianghong\Desktop\PayU需要处理\店铺订单.xlsx");

            this.Logger.LogInformation("下载完成");

            return Ok();
        }

        /// <summary>
        /// 修复PayU订单数据
        /// </summary>
        /// <returns></returns>
        [Route("XiuFuPayUOrder")]
        [HttpGet]
        public async Task<IActionResult> XiuFuPayUOrder()
        {
            List<ShopOrder> orderList = new List<ShopOrder>(500);

            string contentRootPath = this.WebHostEnvironment.ContentRootPath;

            string testFilePath = @"C:\Users\lixianghong\Desktop\PayU需要处理\PayU店铺订单.xlsx";
            testFilePath = @"C:\Users\lixianghong\Desktop\PayU需要处理\PayU店铺订单_处理建议_203628.xlsx";
            List<ShopOrder> payCompanyOrderList = this.ExcelHelper.ReadTitleDataList<ShopOrder>(testFilePath, new ExcelFileDescription(0));

            int dataPosition = 0;
            int dataTotalCount = payCompanyOrderList.Count;
            foreach (ShopOrder shopOrder in payCompanyOrderList)
            {
                dataPosition++;
                this.Logger.LogInformation($"正在处理第{dataPosition}/{dataTotalCount}个数据...");

                if (shopOrder.Remark.IsNotNullOrEmpty())
                {
                    continue;
                }
                List<string> orderRemarkList = new List<string>(3);

                MeShopOrder meShopOrder = null;
                try
                {
                    meShopOrder = await this.MeShopHelper.GetOrder(shopOrder.SiteCode, Convert.ToInt32(shopOrder.OrderCode.Split(',')[0]));
                    if (meShopOrder == null)
                    {
                        orderRemarkList.Add("补单");
                    }
                    else
                    {
                        int chaHours = (int)(Convert.ToDateTime(shopOrder.ShowCreateTime.ToString("yyyy-MM-dd HH:00:00")) - Convert.ToDateTime(meShopOrder.CreateTime.ToString("yyyy-MM-dd HH:00:00"))).TotalHours;
                        if (chaHours != 0
                            && chaHours != 8
                             && chaHours != -8)
                        {
                            orderRemarkList.Add("补单");
                        }
                        else
                        {
                            if (meShopOrder.OrderItemList.Sum(m => m.SplitPayPrice) + meShopOrder.ShipPrice != meShopOrder.CurrencyTotalPayPrice)
                            {
                                if (meShopOrder.OrderItemList.Count == 2)
                                {
                                    MeShopOrderDetail duoOrderDetail = meShopOrder.OrderItemList.FirstOrDefault(m => m.SplitPayPrice != meShopOrder.CurrencyTotalPayPrice);
                                    if (duoOrderDetail == null)
                                    {
                                        duoOrderDetail = meShopOrder.OrderItemList[1];
                                    }
                                    orderRemarkList.Add($"多1个子单:{duoOrderDetail.ID}");
                                }
                                else if (meShopOrder.OrderItemList.Count > 2)
                                {
                                    orderRemarkList.Add("多N个子单");
                                }
                                else
                                {
                                    orderRemarkList.Add("金额异常");
                                }
                            }
                            if (meShopOrder.State != 2)
                            {
                                orderRemarkList.Add("改状态");
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    orderRemarkList.Add("无权限");
                }
                if (orderRemarkList.Count > 0)
                {
                    shopOrder.Remark = string.Join(',', orderRemarkList);
                }
                else
                {
                    shopOrder.Remark = "正常";
                }
            }

            if (false)
            {
                //生成更新订单状态sql
                List<ShopOrder> updateOrderList = payCompanyOrderList.FindAll(m => m.Remark == "改状态");
                Dictionary<string, List<string>> updateOrderStateSqlDic = new Dictionary<string, List<string>>(20);
                foreach (ShopOrder item in updateOrderList)
                {
                    if (!updateOrderStateSqlDic.ContainsKey(item.SiteCode))
                    {
                        updateOrderStateSqlDic.Add(item.SiteCode, new List<string>(10));
                    }
                    updateOrderStateSqlDic[item.SiteCode].Add($"update order_master where state=2,tx='{item.TX}' where id={item.OrderCode.Split(',')[0]};");
                }

                string updateOrderSql = JsonConvert.SerializeObject(updateOrderStateSqlDic);
            }

            IWorkbook workBook = this.ExcelHelper.CreateOrUpdateWorkbook(payCompanyOrderList);
            this.ExcelHelper.SaveWorkbookToFile(workBook, $@"C:\Users\lixianghong\Desktop\PayU需要处理\PayU店铺订单_处理建议_{DateTime.Now.ToString("HHmmss")}.xlsx");

            this.Logger.LogInformation("分析完成");

            return Ok();
        }

        /// <summary>
        /// 生成GCash订单表格
        /// </summary>
        /// <returns></returns>
        [Route("BuildGCashOrderTable")]
        [HttpGet]
        public async Task<IActionResult> BuildGCashOrderTable()
        {
            List<ShopOrder> orderList = new List<ShopOrder>(500);

            string contentRootPath = this.WebHostEnvironment.ContentRootPath;
            string logPath = $@"{contentRootPath}\示例测试目录\支付公司导出订单\gcashpay.log";

            List<string> lineLogList = new List<string>(100000);
            using (StreamReader readStream = new StreamReader(logPath))
            {
                string lineContent = null;
                do
                {
                    lineContent = readStream.ReadLine();
                    if (lineContent.IsNotNullOrEmpty())
                    {
                        lineLogList.Add(lineContent);
                    }
                } while (lineContent.IsNotNullOrEmpty());
            }

            string orderPath = $@"{contentRootPath}\示例测试目录\支付公司导出订单\gcash11.1-11.2订单.xlsx";

            List<PayCompanyOrder> payCompanyOrderList = this.ExcelHelper.ReadTitleDataList<PayCompanyOrder>(orderPath, new ExcelFileDescription(0));

            int dataPosition = 0;
            int dataTotalCount = payCompanyOrderList.Count;
            foreach (PayCompanyOrder item in payCompanyOrderList)
            {
                dataPosition++;
                this.Logger.LogInformation($"正在处理第{dataPosition}/{dataTotalCount}个数据...");

                if (item.TX.IsNotNullOrEmpty())
                {
                    string lineLog = lineLogList.FirstOrDefault(m => m.Contains(item.TX));
                    ShopOrder shopOrder = await this.GetShopOrder(lineLog, null, item.TX, true);

                    lineLog = lineLogList.FindAll(m => m.Contains(shopOrder.SessionID)).FirstOrDefault(m => m.Contains("HomeController,Index,1_收到调度转发请求"));
                    shopOrder = await this.GetShopOrder(lineLog, null, item.TX, true);

                    orderList.Add(shopOrder);
                }
            }

            IWorkbook workBook = this.ExcelHelper.CreateOrUpdateWorkbook(orderList);
            this.ExcelHelper.SaveWorkbookToFile(workBook, @"C:\Users\lixianghong\Desktop\GCash需要处理\店铺订单.xlsx");

            this.Logger.LogInformation("下载完成");

            return Ok();
        }

        /// <summary>
        /// 修复GCash订单数据
        /// </summary>
        /// <returns></returns>
        [Route("XiuFuGCashOrder")]
        [HttpGet]
        public async Task<IActionResult> XiuFuGCashOrder()
        {
            List<ShopOrder> orderList = new List<ShopOrder>(500);

            string contentRootPath = this.WebHostEnvironment.ContentRootPath;

            string testFilePath = @"C:\Users\lixianghong\Desktop\GCash需要处理\GCash店铺订单.xlsx";
            List<ShopOrder> payCompanyOrderList = this.ExcelHelper.ReadTitleDataList<ShopOrder>(testFilePath, new ExcelFileDescription(0));

            int dataPosition = 0;
            int dataTotalCount = payCompanyOrderList.Count;
            foreach (ShopOrder shopOrder in payCompanyOrderList)
            {
                dataPosition++;
                this.Logger.LogInformation($"正在处理第{dataPosition}/{dataTotalCount}个数据...");

                List<string> orderRemarkList = new List<string>(3);

                MeShopOrder meShopOrder = null;
                try
                {
                    meShopOrder = await this.MeShopHelper.GetOrder(shopOrder.SiteCode, Convert.ToInt32(shopOrder.OrderCode.Split(',')[0]));
                    if (meShopOrder == null)
                    {
                        orderRemarkList.Add("补单");
                    }
                    else
                    {
                        int chaHours = (int)(Convert.ToDateTime(shopOrder.ShowCreateTime.ToString("yyyy-MM-dd HH:00:00")) - Convert.ToDateTime(meShopOrder.CreateTime.ToString("yyyy-MM-dd HH:00:00"))).TotalHours;
                        if (chaHours != 0
                            && chaHours != 8
                             && chaHours != -8)
                        {
                            orderRemarkList.Add("补单");
                        }
                        else
                        {
                            if (meShopOrder.OrderItemList.Sum(m => m.SplitPayPrice) + meShopOrder.ShipPrice != meShopOrder.CurrencyTotalPayPrice)
                            {
                                if (meShopOrder.OrderItemList.Count == 2)
                                {
                                    MeShopOrderDetail duoOrderDetail = meShopOrder.OrderItemList.FirstOrDefault(m => m.SplitPayPrice != meShopOrder.CurrencyTotalPayPrice);
                                    if (duoOrderDetail == null)
                                    {
                                        duoOrderDetail = meShopOrder.OrderItemList[1];
                                    }
                                    orderRemarkList.Add($"多1个子单:{duoOrderDetail.ID}");
                                }
                                else if (meShopOrder.OrderItemList.Count > 2)
                                {
                                    orderRemarkList.Add("多N个子单");
                                }
                                else
                                {
                                    orderRemarkList.Add("金额异常");
                                }
                            }
                            if (meShopOrder.State != 2)
                            {
                                orderRemarkList.Add("改状态");
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    orderRemarkList.Add("无权限");
                }
                if (orderRemarkList.Count > 0)
                {
                    shopOrder.Remark = string.Join(',', orderRemarkList);
                }
                else
                {
                    shopOrder.Remark = "正常";
                }
            }

            IWorkbook workBook = this.ExcelHelper.CreateOrUpdateWorkbook(payCompanyOrderList);
            this.ExcelHelper.SaveWorkbookToFile(workBook, $@"C:\Users\lixianghong\Desktop\GCash需要处理\GCash店铺订单_处理建议_{DateTime.Now.ToString("HHmmss")}.xlsx");

            this.Logger.LogInformation("分析完成");

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
                    List<ESLog> esLogList = await this.ESSearchHelper.GetESLogList($"获取调度数据", "meshopstore.com", dataFilter, logDays, log =>
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
