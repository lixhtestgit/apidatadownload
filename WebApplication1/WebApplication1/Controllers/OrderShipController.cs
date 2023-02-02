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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.BIZ;
using WebApplication1.Helper;
using WebApplication1.Model.ExcelModel;
using WebApplication1.Model.MeShop;
using WebApplication1.Model.PayNotify;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 订单发货数据导出控制器
    /// </summary>
    [Route("api/OrderShip")]
    [ApiController]
    public class OrderShipController : ControllerBase
    {
        protected HttpClient PayHttpClient;
        public ExcelHelper ExcelHelper;
        public IWebHostEnvironment WebHostEnvironment;
        public ILogger Logger;
        public IConfiguration Configuration;
        public ESSearchHelper ESSearchHelper;
        public IMemoryCache MemoryCache;
        public AuthBIZ AuthBIZ;

        private MeShopHelper MeShopHelper;

        public OrderShipController(
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            ILogger<OrderShipController> logger,
            IConfiguration configuration,
            ESSearchHelper eSSearchHelper,
            IMemoryCache memoryCache,
            AuthBIZ authBIZ,
            MeShopHelper meShopHelper)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
            this.Logger = logger;
            this.Configuration = configuration;
            this.ESSearchHelper = eSSearchHelper;
            this.MemoryCache = memoryCache;
            this.AuthBIZ = authBIZ;
            this.MeShopHelper = meShopHelper;
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
            List<MeShopOrderShip> orderShipList = await this.getOrderShip();
            IWorkbook workbook = ExcelHelper.CreateOrUpdateWorkbook(orderShipList);
            ExcelHelper.SaveWorkbookToFile(workbook, filePath);

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        /// <summary>
        /// 发送Excel订单发货记录到MeShop站点
        /// api/OrderShip/SendOrderShipToMeShop
        /// </summary>
        /// <returns></returns>
        [Route("SendOrderShipToMeShop")]
        [HttpGet]
        public async Task SendOrderShipToMeShop()
        {
            string contentRootPath = this.WebHostEnvironment.ContentRootPath;
            string waitSyncShipDirectoryPath = $@"{contentRootPath}\示例测试目录\订单发货专用文件夹";
            string[] waitSyncShipFiles = Directory.GetFiles(waitSyncShipDirectoryPath);

            int syncFilePosition = 0;
            int syncTotalFileCount = waitSyncShipFiles.Length;

            foreach (var waitSyncShipFilePath in waitSyncShipFiles)
            {
                syncFilePosition++;

                ExcelOrderShipFile currentFile = this.ExcelHelper.ReadCellData<ExcelOrderShipFile>(waitSyncShipFilePath);
                string hostAdmin = currentFile.ShopUrl.Replace(" ", "").Replace("https://", "").Split('.')[0];

                //获取Excel发货订单数据
                List<ExcelOrderShip> allOrderShipList = this.ExcelHelper.ReadTitleDataList<ExcelOrderShip>(waitSyncShipFilePath, new ExcelFileDescription(1));
                long[] orderIDS = allOrderShipList.Select(m => m.OrderID).Distinct().ToArray();

                //过滤已发货订单
                List<MeShopOrder> meshopOrderList = await this.MeShopHelper.GetOrderList(hostAdmin, orderIDS);
                long[] hadShipedOrders = meshopOrderList.FindAll(m => m.ShipState > 0).Select(m => m.ID).Distinct().ToArray();
                allOrderShipList.RemoveAll(m => hadShipedOrders.Contains(m.OrderID));
                orderIDS = allOrderShipList.Select(m => m.OrderID).Distinct().ToArray();

                List<ExcelOrderShip> orderShipList = null;
                int orderIDIndex = 0;
                foreach (var orderID in orderIDS)
                {
                    orderIDIndex++;
                    if (orderIDIndex <= 0)
                    {
                        continue;
                    }
                    orderShipList = allOrderShipList.FindAll(m => m.OrderID == orderID);

                    JObject orderShipItemJObj = new JObject();
                    foreach (var orderShip in orderShipList)
                    {
                        orderShipItemJObj.Add(orderShip.OrderItemID.ToString(), orderShip.OrderItemProductCount);
                    }

                    dynamic orderShipBody = new
                    {
                        orderID = orderID,
                        ShipNumber = orderShipList[0].ShipNo,
                        ShipUrl = orderShipList[0].ShipNoSearchWebsite,
                        FreightName = orderShipList[0].FreightName,
                        OrderItemCounts = orderShipItemJObj
                    };

                    //尝试最多10次
                    int syncResult = 0;
                    for (int i = 0; i < 10; i++)
                    {
                        syncResult = await this.MeShopHelper.SyncOrderShipToShop(hostAdmin, JsonConvert.SerializeObject(orderShipBody));
                        if (syncResult > 0)
                        {
                            break;
                        }
                    }
                    if (syncResult <= 0)
                    {
                        this.Logger.LogInformation($"同步第{syncFilePosition}/{syncTotalFileCount}个文件第{orderIDIndex}/{orderIDS.Length}个订单发货记录...失败.orderID={orderID}");
                    }
                    else
                    {
                        this.Logger.LogInformation($"已同步第{syncFilePosition}/{syncTotalFileCount}个文件第{orderIDIndex}/{orderIDS.Length}个订单发货记录...");
                    }
                }
            }
            this.Logger.LogInformation($"同步结束.");
        }

        /// <summary>
        /// 获取订单发货数据
        /// </summary>
        /// <returns></returns>
        private async Task<List<MeShopOrderShip>> getOrderShip()
        {
            string shipOrderUrl = "https://namejiu.meshopstore.com/api/v1/order/getorderlist";
            Dictionary<string, string> headDic = await this.AuthBIZ.GetShopAuthDic("namejiu.meshopstore.com", "chenfei@meshop.net", "JISHUchenfei0411");
            string shipOrderPostData = JsonConvert.SerializeObject(new
            {
                ShipState = new { value = new int[] { 1, 2 } },
                pager = new { PageNumber = 1, PageSize = 20000 }
            });
            var shipOrderResult = await this.PayHttpClient.PostJson(shipOrderUrl, shipOrderPostData, headDic);

            JArray shipOrderJArray = JObject.Parse(shipOrderResult.Item2).SelectToken("data.Results").ToObject<JArray>() ?? new JArray();
            List<MeShopOrderShip> orderShipList = new List<MeShopOrderShip>(shipOrderJArray.Count);
            string orderDetailBaseUrl = "https://namejiu.meshopstore.com/api/v1/order/GetOrderDetailPageData?orderID={orderID}";
            string orderDetailUrl = null;
            string orderID = null;
            foreach (JObject itemJObj in shipOrderJArray)
            {
                orderID = itemJObj.SelectToken("ID").ToObject<string>();
                orderDetailUrl = orderDetailBaseUrl.Replace("{orderID}", orderID);

                var orderDetailResult = await this.PayHttpClient.PostJson(orderDetailUrl, "", headDic);
                JArray orderItemListJArray = JObject.Parse(orderDetailResult.Item2).SelectToken("data.OrderItemList").ToObject<JArray>();
                orderShipList.Add(new MeShopOrderShip
                {
                    OrderID = orderID,
                    FreightNameList = orderItemListJArray.Select(m => m.SelectToken("FreightName").ToObject<string>()).ToList()
                });
            }

            return orderShipList;
        }



    }
}
