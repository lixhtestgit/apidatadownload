using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.DB.MeShop;
using WebApplication1.DB.Repository;
using WebApplication1.Helper;
using WebApplication1.Model.MeShopNew;
using static Supabase.Postgrest.Constants;
using static WebApplication1.Enum.EMeShopOrder;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// MeShopNew
    /// </summary>
    [Route("api/MeShopNew")]
    [ApiController]
    public class MeShopNewController : ControllerBase
    {
        public IWebHostEnvironment WebHostEnvironment;
        protected HttpClient PayHttpClient;
        public ExcelHelper ExcelHelper;
        private readonly MeShopNewHelper meShopNewHelper;
        private readonly MeShopNewOrderMasterRepository meShopNewOrderMasterRepository;
        private readonly MeShopNewOrderItemRepository meShopNewOrderItemRepository;
        private readonly MeShopNewOrderAddressRepository meShopNewOrderAddressRepository;
        public ILogger Logger;

        public MeShopNewController(
            IWebHostEnvironment webHostEnvironment,
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            MeShopNewHelper meShopNewHelper,
            MeShopNewOrderMasterRepository meShopNewOrderMasterRepository,
            MeShopNewOrderItemRepository meShopNewOrderItemRepository,
            MeShopNewOrderAddressRepository meShopNewOrderAddressRepository,
            ILogger<MeShopCheckoutController> logger)
        {
            this.WebHostEnvironment = webHostEnvironment;
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.meShopNewHelper = meShopNewHelper;
            this.meShopNewOrderMasterRepository = meShopNewOrderMasterRepository;
            this.meShopNewOrderItemRepository = meShopNewOrderItemRepository;
            this.meShopNewOrderAddressRepository = meShopNewOrderAddressRepository;
            this.Logger = logger;
        }

        /// <summary>
        /// 保存菜单
        /// api/MeShopNew/SaveMenu
        /// </summary>
        /// <returns></returns>
        [Route("SaveMenu")]
        [HttpGet]
        public async Task<IActionResult> SaveMenu()
        {
            string contentRootPath = this.WebHostEnvironment.ContentRootPath;
            string filePath = $@"{contentRootPath}\示例测试目录\MeShopNew\权限整理11.xlsx";

            //清理旧数据
            await this.meShopNewHelper.ExecSqlToShop("meshop001", 0, "delete from base_permission where 1=1");

            List<MeShopNewMenu> meShopNewMenuList = ExcelHelper.ReadTitleDataList<MeShopNewMenu>(filePath, new ExcelFileDescription());
            List<string> insertSqlList = new List<string>();
            foreach (MeShopNewMenu menu in meShopNewMenuList)
            {
                if (string.IsNullOrWhiteSpace(menu.ID))
                {
                    menu.ID = Guid.NewGuid().ToString();
                }

                //添加新数据
                insertSqlList.Add($"('{menu.ID}','{menu.ParentID}',{menu.Type},'{menu.Name}','{menu.Icon}','{menu.Href}',{menu.IsEnable},{menu.Sort},'2024-04-28')");
            }

            string insertSql = $"insert into base_permission(ID,ParentID,Type,Name,Icon,Href,IsEnable,Sort,CreateTime) Values {string.Join(',', insertSqlList)};";
            await this.meShopNewHelper.ExecSqlToShop("meshop001", 0, insertSql);

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        /// <summary>
        /// 同步用户和产品
        /// api/MeShopNew/SyncUserAndProduct
        /// </summary>
        /// <returns></returns>
        [Route("SyncUserAndProduct")]
        [HttpGet]
        public async Task<IActionResult> SyncUserAndProduct()
        {
            IDbConnection commonDBConnection = MySqlConnector.MySqlConnectorFactory.Instance.CreateConnection();
            commonDBConnection.ConnectionString = $"Data Source=database-shop-v3.cz0qdgtkh5t0.us-west-2.rds.amazonaws.com;User Id=shop;Password=47LvHJtjbmux6c9piJqt;Port=3306;default command timeout=100;Connection Timeout=30;Charset=utf8mb4;Allow User Variables=true;IgnoreCommandTransaction=true;database=common";

            string querySQL = "select hostadmin from base_shop";
            List<string> hostAdminList = (await commonDBConnection.QueryAsync<string>(querySQL, null, commandTimeout: 120)).ToList();

            List<SyncUserProduct> allList = new List<SyncUserProduct>(100000);

            int execHostAdminCount = 0;
            int totalCount = hostAdminList.Count;
            foreach (string hostAdmin in hostAdminList)
            {
                execHostAdminCount++;
                this.Logger.LogInformation($"正在查询第{execHostAdminCount}/{totalCount}个店铺用户产品数据...");

                commonDBConnection.ConnectionString = $"Data Source=database-shop-v3.cz0qdgtkh5t0.us-west-2.rds.amazonaws.com;User Id=shop;Password=47LvHJtjbmux6c9piJqt;Port=3306;default command timeout=100;Connection Timeout=30;Charset=utf8mb4;Allow User Variables=true;IgnoreCommandTransaction=true;database=s_{hostAdmin}";
                querySQL = @"SELECT MIN(u.CreateTime) CreateTime,
                        u.Email,
                        MIN(u.FirstName) FirstName,
                        MIN(u.LastName) LastName,
                        GROUP_CONCAT(i.Title) ProductTitles,
                        GROUP_CONCAT(CONCAT('https://cdn.mestoresy.com/c',i.Src)) ProductImages,
                        MIN(a.CountryCode) CountryCode
                        from user_info u
                        LEFT JOIN order_master o ON u.ID=o.UserID
                        LEFT JOIN order_item i on i.OrderID=o.ID
                        LEFT JOIN order_address a on a.OrderID=o.ID and a.Type=1
                        GROUP BY u.Email";
                List<SyncUserProduct> hostAdminDataList = (await commonDBConnection.QueryAsync<SyncUserProduct>(querySQL, null, commandTimeout: 120)).ToList();
                if (hostAdminDataList.Count > 0)
                {
                    allList.AddRange(hostAdminDataList);
                }
            }

            try
            {
                IWorkbook workbook = this.ExcelHelper.CreateOrUpdateWorkbook(allList);
                this.ExcelHelper.SaveWorkbookToFile(workbook, @$"C:\Users\lixianghong\Desktop\SyncUserAndProduct_{DateTime.Now.ToString("HHmmss")}.xlsx");
            }
            catch (Exception e)
            {
                throw;
            }

            return Ok();
        }

        /// <summary>
        /// 批量同步假发订单
        /// api/MeShopNew/BatchSyncWigsbuyshopOrder
        /// </summary>
        /// <returns></returns>
        [Route("BatchSyncWigsbuyshopOrder")]
        [HttpGet]
        public async Task<IActionResult> BatchSyncWigsbuyshopOrder()
        {
            //DELETE from order_address where OrderID in (select id from order_master where CreateTime > '2024-01-01' and CreateTime<'2025-01-01');
            //DELETE from order_item where OrderID in (select id from order_master where CreateTime > '2024-01-01' and CreateTime<'2025-01-01');
            //DELETE from order_master where CreateTime > '2024-01-01' and CreateTime<'2025-01-01';


            //select A.CreateTime '月份',Count(1) '订单总量',SUM(TotalPayPrice) '订单总金额',SUM(TotalPayPrice) / Count(1) '平均客单价' from(
            //select date_format(CreateTime, '%Y-%m') CreateTime, TotalPayPrice, 1 from order_master where state in (2, 4)
            //) A GROUP BY A.CreateTime

            //设置已付款中1315个已取消订单
            if (false)
            {
                List<long> setCanceledOrderIDList = new List<long>();

                int canceled = 1299;

                List<long> orderIDList = (await this.meShopNewOrderMasterRepository.QueryAsync<long>(Enum.EDBSiteName.Wigsbuyshop, $"select id from order_master where state in (2,4) and CreateTime<'2025-01-01'"));

                Random random = new Random();

                do
                {
                    int ranIndex = random.Next(0, orderIDList.Count);
                    long ranOrderID = orderIDList[ranIndex];

                    if (!setCanceledOrderIDList.Contains(ranOrderID))
                    {
                        setCanceledOrderIDList.Add(ranOrderID);
                    }
                } while (setCanceledOrderIDList.Count < canceled);

                string updateSql = $"update order_master set state=5 where id in ({string.Join(',', setCanceledOrderIDList)})";
                await this.meShopNewOrderMasterRepository.ExecuteAsync(Enum.EDBSiteName.Wigsbuyshop, updateSql, null);
            }

            //设置已付款中4000个发货订单
            if (false)
            {
                List<long> setShipedOrderIDList = new List<long>();

                int shiped = 4000;

                List<long> orderIDList = (await this.meShopNewOrderMasterRepository.QueryAsync<long>(Enum.EDBSiteName.Wigsbuyshop, $"select id from order_master where state in (2,4) and FulfillmentState=0 and CreateTime<'2025-01-01'"));

                Random random = new Random();

                do
                {
                    int ranIndex = random.Next(0, orderIDList.Count);
                    long ranOrderID = orderIDList[ranIndex];

                    if (!setShipedOrderIDList.Contains(ranOrderID))
                    {
                        setShipedOrderIDList.Add(ranOrderID);
                    }
                } while (setShipedOrderIDList.Count < shiped);

                string updateSql = $"update order_master set FulfillmentState=2 where id in ({string.Join(',', setShipedOrderIDList)})";
                await this.meShopNewOrderMasterRepository.ExecuteAsync(Enum.EDBSiteName.Wigsbuyshop, updateSql, null);
            }

            //设置已付款订单全部位已完成
            if (false)
            {
                List<long> orderIDList = (await this.meShopNewOrderMasterRepository.QueryAsync<long>(Enum.EDBSiteName.Wigsbuyshop, $"select id from order_master where state in (2) and CreateTime<'2025-01-01'"));

                string updateSql = $"update order_master set state=4 where id in ({string.Join(',', orderIDList)})";
                await this.meShopNewOrderMasterRepository.ExecuteAsync(Enum.EDBSiteName.Wigsbuyshop, updateSql, null);
            }

            //同步2025年数据到2024年
            if (false)
            {
                DateTime syncBeinTime = TypeParseHelper.StrToDateTime("2025-01-01");
                List<Order_master> syncOrderMasterList = await this.meShopNewOrderMasterRepository.SelectAsync<Order_master>(Enum.EDBSiteName.Wigsbuyshop, m => m.State == 2 && m.CreateTime > syncBeinTime);
                syncOrderMasterList = syncOrderMasterList.OrderByDescending(m => m.CreateTime).ToList();

                long[] syncOrderIDS = syncOrderMasterList.Select(m => m.ID).ToArray();

                List<Order_address> syncOrderAddressList = await this.meShopNewOrderMasterRepository.SelectAsync<Order_address>(Enum.EDBSiteName.Wigsbuyshop, m => syncOrderIDS.Contains(m.OrderID));
                List<Order_item> syncOrderItemList = await this.meShopNewOrderMasterRepository.SelectAsync<Order_item>(Enum.EDBSiteName.Wigsbuyshop, m => syncOrderIDS.Contains(m.OrderID));

                Func<int, int, decimal, int, Task> execMonthFunc = async (execYear, execMonth, syncPreOrderUSDPirce, syncSumOrderCount) =>
                {
                    //开始时间
                    DateTime beginTime = TypeParseHelper.StrToDateTime($"{execYear}-{execMonth.ToString("00")}-01");
                    //结束时间
                    DateTime endTime = beginTime.AddMonths(1);

                    Random random = new Random();
                    decimal currentSyncSumTotalUSDPrice = 0;
                    int currentSyncOrderCount = 0;
                    long currentMinOrderID = TypeParseHelper.StrToInt64(await this.meShopNewOrderMasterRepository.ExecuteScalarAsync(Enum.EDBSiteName.Wigsbuyshop, $"select min(id) from order_master", null));

                    DateTime currentOrderTime = endTime;

                    do
                    {
                        List<Order_master> awaitInsertOrderList = new List<Order_master>(syncOrderMasterList.Count);
                        List<Order_address> awaitInsertOrderAddressList = new List<Order_address>(syncOrderAddressList.Count);
                        List<Order_item> awaitInsertOrderItemList = new List<Order_item>(syncOrderItemList.Count);

                        foreach (Order_master syncOrderMaster in syncOrderMasterList)
                        {
                            if (currentSyncOrderCount >= syncSumOrderCount)
                            {
                                break;
                            }

                            decimal bs = syncPreOrderUSDPirce / syncOrderMaster.TotalPayPrice + random.Next(-10, 10) * (decimal)0.001;
                            syncOrderMaster.TotalPayPrice *= bs;
                            syncOrderMaster.TotalOriginalPrice *= bs;
                            syncOrderMaster.ShipPrice *= bs;
                            syncOrderMaster.TaxPrice *= bs;
                            syncOrderMaster.TotalDiscount *= bs;
                            syncOrderMaster.TotalPayCompanyDiscount *= bs;
                            syncOrderMaster.CurrencyTotalPayPrice *= bs;

                            double randomSeconds = (30 * 24 * 3600) / (double)syncSumOrderCount;
                            currentOrderTime = currentOrderTime.AddSeconds(randomSeconds * -1);
                            if (currentOrderTime < beginTime)
                            {
                                currentOrderTime = beginTime;
                            }

                            syncOrderMaster.CreateTime = currentOrderTime;
                            syncOrderMaster.PayTime = syncOrderMaster.CreateTime.AddSeconds(19);
                            syncOrderMaster.CancelTime = null;
                            syncOrderMaster.CompleteTime = null;

                            long originOrderID = syncOrderMaster.ID;

                            currentMinOrderID--;
                            syncOrderMaster.ID = currentMinOrderID;

                            awaitInsertOrderList.Add(syncOrderMaster);

                            if (syncOrderMaster.ID > 0)
                            {
                                currentSyncSumTotalUSDPrice += syncOrderMaster.TotalPayPrice;
                                currentSyncOrderCount++;

                                List<Order_address> currentOrderAddress = syncOrderAddressList.FindAll(m => m.OrderID == originOrderID);
                                foreach (Order_address item in currentOrderAddress)
                                {
                                    item.ID = 0;
                                    item.OrderID = syncOrderMaster.ID;
                                }
                                awaitInsertOrderAddressList.AddRange(currentOrderAddress);

                                List<Order_item> currentOrderItemList = syncOrderItemList.FindAll(m => m.OrderID == originOrderID);
                                foreach (Order_item item in currentOrderItemList)
                                {
                                    item.ID = 0;
                                    item.OrderID = syncOrderMaster.ID;
                                    item.SellPrice *= bs;
                                    item.SplitPayPrice *= bs;
                                }
                                awaitInsertOrderItemList.AddRange(currentOrderItemList);

                            }

                            Console.WriteLine($"当前同步{execYear}年{execMonth}月订单量进度：{currentSyncOrderCount}/{syncSumOrderCount}");
                        }

                        await this.meShopNewOrderMasterRepository.InsertList(Enum.EDBSiteName.Wigsbuyshop, awaitInsertOrderList);
                        await this.meShopNewOrderAddressRepository.InsertList(Enum.EDBSiteName.Wigsbuyshop, awaitInsertOrderAddressList);
                        await this.meShopNewOrderItemRepository.InsertList(Enum.EDBSiteName.Wigsbuyshop, awaitInsertOrderItemList);

                    } while (currentSyncOrderCount < syncSumOrderCount);
                };


                var monthExecList = new dynamic[] {
                new {
                    execYear = 2024,
                    execMonth = 12,
                    //需要同步的客单价
                    syncPreOrderUSDPirce = 376.32,
                    //需要同步的订单量
                    syncSumOrderCount = 3284
                },
                new {
                    execYear = 2024,
                    execMonth = 11,
                    //需要同步的客单价
                    syncPreOrderUSDPirce = 398.75,
                    //需要同步的订单量
                    syncSumOrderCount = 5737
                },
                new {
                    execYear = 2024,
                    execMonth = 10,
                    //需要同步的客单价
                    syncPreOrderUSDPirce = 392.68,
                    //需要同步的订单量
                    syncSumOrderCount = 7025
                },
                new {
                    execYear = 2024,
                    execMonth = 9,
                    //需要同步的客单价
                    syncPreOrderUSDPirce = 359.17,
                    //需要同步的订单量
                    syncSumOrderCount = 3034
                },
                new {
                    execYear = 2024,
                    execMonth = 8,
                    //需要同步的客单价
                    syncPreOrderUSDPirce = 368.42,
                    //需要同步的订单量
                    syncSumOrderCount = 3138
                },
                new {
                    execYear = 2024,
                    execMonth = 7,
                    //需要同步的客单价
                    syncPreOrderUSDPirce = 352.89,
                    //需要同步的订单量
                    syncSumOrderCount = 2907
                },
                new {
                    execYear = 2024,
                    execMonth = 6,
                    //需要同步的客单价
                    syncPreOrderUSDPirce = 319.57,
                    //需要同步的订单量
                    syncSumOrderCount = 1430
                },
                new {
                    execYear = 2024,
                    execMonth = 5,
                    //需要同步的客单价
                    syncPreOrderUSDPirce = 335.64,
                    //需要同步的订单量
                    syncSumOrderCount = 2182
                },
                new {
                    execYear = 2024,
                    execMonth = 4,
                    //需要同步的客单价
                    syncPreOrderUSDPirce = 308.21,
                    //需要同步的订单量
                    syncSumOrderCount = 1387
                },
                new {
                    execYear = 2024,
                    execMonth = 3,
                    //需要同步的客单价
                    syncPreOrderUSDPirce = 325.46,
                    //需要同步的订单量
                    syncSumOrderCount = 1593
                },
                new {
                    execYear = 2024,
                    execMonth = 2,
                    //需要同步的客单价
                    syncPreOrderUSDPirce = 318.72,
                    //需要同步的订单量
                    syncSumOrderCount = 1452
                },
                new {
                    execYear = 2024,
                    execMonth = 1,
                    //需要同步的客单价
                    syncPreOrderUSDPirce = 312.58,
                    //需要同步的订单量
                    syncSumOrderCount = 1566
                }
            };

                foreach (var item in monthExecList)
                {
                    JObject itemJobj = JObject.FromObject(item);
                    int execYear = itemJobj.SelectToken("execYear").ToObject<int>();
                    int execMonth = itemJobj.SelectToken("execMonth").ToObject<int>();
                    decimal syncPreOrderUSDPirce = itemJobj.SelectToken("syncPreOrderUSDPirce").ToObject<decimal>();
                    int syncSumOrderCount = itemJobj.SelectToken("syncSumOrderCount").ToObject<int>();

                    await execMonthFunc(execYear, execMonth, syncPreOrderUSDPirce, syncSumOrderCount);

                    Console.WriteLine($"执行{execMonth.ToString("00")}/{execYear}月份数据完成...");
                    await Task.Delay(3000);
                }
            }

            Console.WriteLine("执行结束");

            return Ok();
        }

        #region 扩展类

        private class SyncUserProduct
        {
            [ExcelTitle("购买时间")]
            public DateTime CreateTime { get; set; }
            [ExcelTitle("邮箱")]
            public string Email { get; set; }
            [ExcelTitle("名")]
            public string FirstName { get; set; }
            [ExcelTitle("姓")]
            public string LastName { get; set; }
            [ExcelTitle("产品标题")]
            public string ProductTitles { get; set; }
            [ExcelTitle("产品图片")]
            public string ProductImages { get; set; }
            [ExcelTitle("国家编码")]
            public string CountryCode { get; set; }
        }
        #endregion
    }
}
