using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication1.DB.Base;
using WebApplication1.DB.CMS;
using WebApplication1.DB.Repository;
using WebApplication1.Enum;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CmsDataExportController : ControllerBase
    {
        //api/CmsDataExport/CopyDataToSite
        private BaseRepository _baseRepository;
        private TB_UsersRepository _usersRepository;
        private TJ_TB_OrderRepository _tjOrderRepository;
        private TB_OrderBillRepository _orderBillRepository;
        private TB_UserSendAddressOrderRepository _userSendAddressOrderRepository;
        private ExcelHelper _excelHelper;
        private ILogger _logger;
        private object _obj = new object();

        public CmsDataExportController(
            BaseRepository baseRepository,
            TB_UsersRepository usersRepository,
            TJ_TB_OrderRepository orderRepository,
            TB_OrderBillRepository orderBillRepository,
            TB_UserSendAddressOrderRepository userSendAddressOrderRepository,
            ExcelHelper excelHelper,
            ILogger<TestController> logger)
        {
            this._baseRepository = baseRepository;
            this._usersRepository = usersRepository;
            this._tjOrderRepository = orderRepository;
            this._orderBillRepository = orderBillRepository;
            this._userSendAddressOrderRepository = userSendAddressOrderRepository;
            this._excelHelper = excelHelper;
            this._logger = logger;
        }

        /// <summary>
        /// 复制CMS站点数据到独立做账站点订单表中
        /// </summary>
        /// <returns></returns>
        [Route("CopyDataToSite")]
        [HttpGet]
        public async Task CopyDataToSite()
        {
            string beginDate = "2022-09-01";
            string endDate = "2022-10-01";

            #region 需求1：排除指定站点（0,1,11,1363,18,19,195,27,34,35,36,37,41,43,6689,6874,6876,6880,6881,6916,7162,7163,7003,7143）平均分配到对应10个站点中

            string tjOrderSql = @$"SELECT u.Email UserEmail,
                            (SELECT SUM(ob.BuyCount) FROM dbo.TB_OrderBill ob WHERE ob.SiteID=o.SiteID AND ob.OrderID=o.ID) ProductCount,
                            a.CountryID,
                            o.* FROM dbo.TB_Order o
                            LEFT JOIN dbo.TB_Site s ON s.ID=o.SiteID
                            LEFT JOIN dbo.TB_Users AS u ON o.UserID = U.ID AND o.SiteID = u.SiteID
                            LEFT JOIN dbo.TB_UserSendAddressOrder AS a WITH (NOLOCK) ON o.SiteID = a.SiteID AND a.OrderID = o.ID AND a.AddressID = o.Address1
                            WHERE o.AddTime>'{beginDate}' AND o.AddTime<'{endDate}' AND o.SiteID NOT IN (0,1,11,1363,18,19,195,27,34,35,36,37,41,43,6689,6874,6876,6880,6881,6916,7162,7163,7003,7143)
                            Order By o.AddTime";
            List<TJ_TB_Order> awaitOrderJObjList = this._baseRepository.QueryAsync<TJ_TB_Order>(EDBConnectionType.SqlServer, tjOrderSql).Result;

            Random random = new Random();
            int orderObjPosition = 0;
            int orderObjTotalCount = awaitOrderJObjList.Count;

            //同步原始订单到新站点
            Dictionary<int, int> siteZB = new Dictionary<int, int>
            {
                {6546,23 },
                {6938,23 },
                {6903,17 },
                {6691,17 },
                {7027,6 },
                {7207,2 },
                {7211,2 },
                {6738,2 },
                {6983,2 },
                {7204,2 },
                {7203,2 },
                {7224,2 }
            };
            Dictionary<int, int> siteMaxOrderIDDic = new Dictionary<int, int>(12);
            foreach (var item in siteZB)
            {
                int maxOrderID = await this._tjOrderRepository.GetMaxID(item.Key);
                siteMaxOrderIDDic.Add(item.Key, maxOrderID);
            }

            Func<TJ_TB_Order, Task> syncOrderToNewSiteFunc = async (orderObj) =>
            {
                try
                {
                    orderObjPosition++;
                    this._logger.LogInformation($"正在同步需求1第{orderObjPosition}/{orderObjTotalCount}个订单...");

                    int fpSiteID = 0;
                    int randomI = random.Next(1, 101);
                    foreach (var item in siteZB)
                    {
                        randomI = randomI - item.Value;
                        if (randomI <= 0)
                        {
                            fpSiteID = item.Key;
                            break;
                        }
                    }

                    //添加订单
                    orderObj.OriginID = orderObj.ID;
                    orderObj.OriginSiteID = orderObj.SiteID;
                    orderObj.SiteID = fpSiteID;

                    int insertResult = 0;
                    int fpOrderID = 0;
                    do
                    {
                        try
                        {
                            lock (this._obj)
                            {
                                fpOrderID = siteMaxOrderIDDic[fpSiteID] + 1;
                                siteMaxOrderIDDic[fpSiteID] = fpOrderID;
                            }
                            orderObj.ID = fpOrderID;
                            insertResult = await this._tjOrderRepository.Insert(EDBConnectionType.SqlServer, orderObj);
                        }
                        catch (Exception)
                        {
                        }
                    } while (insertResult == 0);
                }
                catch (Exception e)
                {
                    throw;
                }
            };

            int waitObjIndex = 0;
            List<Task> syncTaskList = new List<Task>(10);
            bool allIsSync = true;
            do
            {
                allIsSync = true;
                if (syncTaskList.Count < 10 && waitObjIndex <= awaitOrderJObjList.Count - 1)
                {
                    Task syncTask = syncOrderToNewSiteFunc(awaitOrderJObjList[waitObjIndex]);
                    syncTaskList.Add(syncTask);
                    waitObjIndex++;
                }
                //检测异步方法是否全部执行完毕
                allIsSync = syncTaskList.Exists(m => m.IsCompleted == false) ? false : true;
                //移除已完成任务，安排下一对象同步
                syncTaskList.RemoveAll(x => x.IsCompleted == true);
            } while (allIsSync == false || waitObjIndex < awaitOrderJObjList.Count);

            #endregion

            #region 需求2：将www.beddinginn.com,www.oroyalcars.com(19,1363)将原始订单数据直接搬迁到独立表

            tjOrderSql = @$"SELECT u.Email UserEmail,
                            (SELECT SUM(ob.BuyCount) FROM dbo.TB_OrderBill ob WHERE ob.SiteID=o.SiteID AND ob.OrderID=o.ID) ProductCount,
                            a.CountryID,
                            o.* FROM dbo.TB_Order o
                            LEFT JOIN dbo.TB_Site s ON s.ID=o.SiteID
                            LEFT JOIN dbo.TB_Users AS u ON o.UserID = U.ID AND o.SiteID = u.SiteID
                            LEFT JOIN dbo.TB_UserSendAddressOrder AS a WITH (NOLOCK) ON o.SiteID = a.SiteID AND a.OrderID = o.ID AND a.AddressID = o.Address1
                            WHERE o.AddTime>'{beginDate}' AND o.AddTime<'{endDate}' AND o.SiteID IN (19,1363)
                            Order By o.AddTime";
            awaitOrderJObjList = this._baseRepository.QueryAsync<TJ_TB_Order>(EDBConnectionType.SqlServer, tjOrderSql).Result;
            orderObjTotalCount = awaitOrderJObjList.Count;
            orderObjPosition = 0;

            //同步原始订单到原站点
            Func<TJ_TB_Order, Task> syncOrderFunc = async (orderObj) =>
            {
                try
                {
                    orderObjPosition++;
                    this._logger.LogInformation($"正在同步需求2第{orderObjPosition}/{orderObjTotalCount}个订单...");

                    //添加订单
                    orderObj.OriginID = orderObj.ID;
                    orderObj.OriginSiteID = orderObj.SiteID;

                    int insertResult = 0;
                    do
                    {
                        try
                        {
                            insertResult = await this._tjOrderRepository.Insert(EDBConnectionType.SqlServer, orderObj);
                        }
                        catch (Exception)
                        {
                        }
                    } while (insertResult == 0);
                }
                catch (Exception e)
                {
                    throw;
                }
            };

            waitObjIndex = 0;
            syncTaskList = new List<Task>(10);
            allIsSync = true;
            do
            {
                allIsSync = true;
                if (syncTaskList.Count < 10 && waitObjIndex <= awaitOrderJObjList.Count - 1)
                {
                    Task syncTask = syncOrderFunc(awaitOrderJObjList[waitObjIndex]);
                    syncTaskList.Add(syncTask);
                    waitObjIndex++;
                }
                //检测异步方法是否全部执行完毕
                allIsSync = syncTaskList.Exists(m => m.IsCompleted == false) ? false : true;
                //移除已完成任务，安排下一对象同步
                syncTaskList.RemoveAll(x => x.IsCompleted == true);
            } while (allIsSync == false || waitObjIndex < awaitOrderJObjList.Count);

            #endregion

            #region 需求3

            //执行SQL查询数据整理到Excel中：
            var huizongSQL = @"
            SELECT ID, Name,
            (SELECT SUM(Price_PreCount1) FROM dbo.TJ_TB_Order WHERE SiteID = TB_Site.ID AND AddTime>= '2022-09-01' AND AddTime<'2022-10-01')[9月美金汇总]
            FROM TB_Site where ID IN(6546, 6691, 6738, 6903, 6938, 6983, 7027, 7203, 7204, 7207, 7211, 7224)
            ORDER BY ID
            ";

            var xiangqingSQL = @"
            select o.SiteID[站点ID],
                (SELECT s.Name FROM dbo.TB_Site s WHERE s.ID=o.SiteID)[站点名称],
                ID[订单ID],AddTime[创建时间],o.CurrencyName[币种名称],
                CASE WHEN o.CurrencyPrice IS NULL OR o.CurrencyPrice<=0 THEN o.Price_PreCount1 ELSE o.CurrencyPrice END[多币种金额],
                o.Price_PreCount1[美金金额],
                o.OriginSiteID[原始站点ID],o.OriginID[原始订单ID]
            from dbo.TJ_TB_Order o 
            WHERE AddTime>='2022-09-01' and AddTime<'2022-10-01' 
            AND o.SiteID IN (6546,6691,6738,6903,6938,6983,7027,7203,7204,7207,7211,7224)
            ORDER by AddTime
            ";

            #endregion

            this._logger.LogInformation("订单处理完成！");
        }

    }
}
