using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PPPayReportTools.Excel;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebApplication1.Helper;
using WebApplication1.Model.PayNotify;

namespace WebApplication1.Controllers
{
    [Route("api/PayNotify")]
    [ApiController]
    public class PayNotifyController : ControllerBase
    {
        protected HttpClient PayHttpClient { get; set; }
        public ExcelHelper ExcelHelper { get; set; }
        public IWebHostEnvironment WebHostEnvironment { get; set; }
        public ILogger Logger { get; set; }
        public IConfiguration Configuration { get; set; }
        public ESSearchHelper ESSearchHelper { get; set; }

        public PayNotifyController(
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
            ""query"": ""MeShopPay向网站发送支付结果失败"",
            ""lenient"": true
        }
    }
]";

            List<ESLog> esLogList = await this.ESSearchHelper.GetESLogList($"获取Pay通知失败数据", "meshopstore.com", dataFilter, 6, log =>
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
                notifyUrl = notifyUrlRegex.Match(log.Log).Value + "?sessionID=" + sessionID;
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
            string testFilePath = $@"{contentRootPath}\示例测试目录\支付公司导出订单\paytm-订单2.xlsx";
            List<PayCompanyOrder> orderList = this.ExcelHelper.ReadTitleDataList<PayCompanyOrder>(testFilePath, new ExcelFileDescription(0));

            int totalCount = orderList.Count;
            this.Logger.LogInformation($"获取到同步失败数据共{totalCount}个");
            string notifyUrl = null;
            int position = 0;
            bool isSync = false;
            string payCompany = "Paytm";
            int orderSyncCount = 0;
            List<string> syncFailedList = new List<string>(10);
            foreach (PayCompanyOrder order in orderList)
            {
                position++;
                orderSyncCount = 0;
                order.SessionID = order.SessionID.Replace("'", "");
                notifyUrl = $"https://pay.meshopstore.com/Callback/Paytm/Notification/{order.SessionID}";
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
    }
}
