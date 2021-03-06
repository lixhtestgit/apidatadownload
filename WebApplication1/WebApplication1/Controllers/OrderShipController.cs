using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.BIZ;
using WebApplication1.Helper;
using WebApplication1.Model;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 订单发货数据导出控制器
    /// </summary>
    [Route("api/OrderShip")]
    [ApiController]
    public class OrderShipController : ControllerBase
    {
        protected HttpClient PayHttpClient { get; set; }
        public ExcelHelper ExcelHelper { get; set; }
        public IWebHostEnvironment WebHostEnvironment { get; set; }
        public ILogger Logger { get; set; }
        public IConfiguration Configuration { get; set; }
        public ESSearchHelper ESSearchHelper { get; set; }
        public IMemoryCache MemoryCache { get; set; }
        public AuthBIZ AuthBIZ { get; set; }

        public OrderShipController(
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            ILogger<OrderShipController> logger,
            IConfiguration configuration,
            ESSearchHelper eSSearchHelper,
            IMemoryCache memoryCache,
            AuthBIZ authBIZ)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
            this.Logger = logger;
            this.Configuration = configuration;
            this.ESSearchHelper = eSSearchHelper;
            this.MemoryCache = memoryCache;
            this.AuthBIZ = authBIZ;
        }

        /// <summary>
        /// ES搜索订单支付方式
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task<IActionResult> ESSearchOrderPayType()
        {
            string filePath = @"C:\Users\lixianghong\Desktop\Test.xlsx";
            List<OrderShip> orderShipList = await this.GetOrderShip();
            IWorkbook workbook = ExcelHelper.CreateOrUpdateWorkbook(orderShipList);
            ExcelHelper.SaveWorkbookToFile(workbook, filePath);

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        private async Task<List<OrderShip>> GetOrderShip()
        {
            string shipOrderUrl = "https://namejiu.meshopstore.com/api/v1/order/getorderlist";
            Dictionary<string, string> headDic = await this.AuthBIZ.GetShopAuthDic("namejiu.meshopstore.com", "chenfei@meshop.net", "JISHUchenfei0411");
            string shipOrderPostData = JsonConvert.SerializeObject(new
            {
                ShipState = new { value = new int[] { 1, 2 } },
                pager = new { PageNumber = 1, PageSize = 20000 }
            });
            var shipOrderResult = await this.PayHttpClient.Post(shipOrderUrl, shipOrderPostData, headDic);

            JArray shipOrderJArray = JObject.Parse(shipOrderResult.Item2).SelectToken("data.Results").ToObject<JArray>() ?? new JArray();
            List<OrderShip> orderShipList = new List<OrderShip>(shipOrderJArray.Count);
            string orderDetailBaseUrl = "https://namejiu.meshopstore.com/api/v1/order/GetOrderDetailPageData?orderID={orderID}";
            string orderDetailUrl = null;
            string orderID = null;
            foreach (JObject itemJObj in shipOrderJArray)
            {
                orderID = itemJObj.SelectToken("ID").ToObject<string>();
                orderDetailUrl = orderDetailBaseUrl.Replace("{orderID}", orderID);

                var orderDetailResult = await this.PayHttpClient.Post(orderDetailUrl, "", headDic);
                JArray orderItemListJArray = JObject.Parse(orderDetailResult.Item2).SelectToken("data.OrderItemList").ToObject<JArray>();
                orderShipList.Add(new OrderShip
                {
                    OrderID = orderID,
                    FreightNameList = orderItemListJArray.Select(m => m.SelectToken("FreightName").ToObject<string>()).ToList()
                });
            }

            return orderShipList;
        }
    }
}
