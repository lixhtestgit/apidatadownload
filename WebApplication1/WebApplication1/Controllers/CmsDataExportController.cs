using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.DB.Base;
using WebApplication1.DB.CMS;
using WebApplication1.DB.Extend;
using WebApplication1.DB.Repository;
using WebApplication1.Enum;
using WebApplication1.Model;
using WebApplication1.Model.CmsData;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CmsDataExportController : ControllerBase
    {
        private BaseRepository _baseRepository;
        private TB_UsersRepository _usersRepository;
        private TJ_TB_OrderRepository _tjOrderRepository;
        private TB_OrderBillRepository _orderBillRepository;
        private TB_UserSendAddressOrderRepository _userSendAddressOrderRepository;
        private ExcelHelper _excelHelper;
        private ILogger _logger;

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

        [Route("")]
        [HttpGet]
        public async Task DownLoadData()
        {
            string siteName = "de.beddinginn.com";
            int siteID = 45;
            string sql = @"SELECT s.Name SiteName,o.ID OrderID,bill.ID OrderBillID,o.Price_PreCount1,
                    dbo.GetAllCategoryName(pro.ParentIDs,pro.SiteID,',') CategoryName,
                    o.AddTime,pro.SPUID,bill.CustomID SKUID,o.PayTime,
                    (SELECT ptm.PayTypeName FROM TB_PayMethodAndPaymentCompanyMapping ptm WHERE ptm.ID=o.PayType) PayTypeName,
                    addOrder.AddRessUserName UserName,u.RegDate,u.Email,addOrder.AddressLine1,addOrder.AddressLine2,addOrder.Phone,country.CountryName,
                    (SELECT SUM(re.RefundMoney) FROM dbo.TB_OrderRemark re WHERE re.OrderID=o.ID AND re.SiteID=re.SiteID AND re.BillID=bill.ID AND re.State=',5,' AND re.IsRefund=1) RefundPrice,
                    (SELECT TOP 1 re.Remark FROM dbo.TB_OrderRemark re WHERE re.OrderID=o.ID AND re.SiteID=re.SiteID AND re.BillID=bill.ID AND re.State=',5,' AND re.IsRefund=1) RefundReson,
                    bill.Remark OrderRemark
                    FROM
                    dbo.TB_Order o
                    INNER JOIN dbo.TB_Site s ON s.ID = o.SiteID
                    INNER JOIN dbo.TB_UserSendAddressOrder addOrder ON addOrder.OrderID = o.ID AND o.SiteID = addOrder.SiteID AND addOrder.Type = 1
                    INNER JOIN dbo.TB_Country country ON country.ID = addOrder.CountryID
                    INNER JOIN dbo.TB_OrderBill bill ON bill.OrderID = o.ID AND bill.SiteID = o.SiteID
                    INNER JOIN dbo.TB_Users u ON o.UserID = u.ID AND o.SiteID = u.SiteID
                    LEFT JOIN dbo.TB_Product pro ON pro.ID = bill.ProductID AND pro.SiteID = bill.SiteID
                    WHERE {wheresql} AND o.AddTime<'2019-06-01' AND o.SiteID = " + siteID;

            string idSql = $"SELECT MIN(ID) minID,MAX(ID) maxID from dbo.TB_Order WHERE SiteID={siteID}";
            JObject idJObj = (await this._baseRepository.QueryAsync<JObject>(EDBConnectionType.SqlServer, idSql)).FirstOrDefault();
            int minID = idJObj.SelectToken("minID").ToObject<int>();
            int maxID = idJObj.SelectToken("maxID").ToObject<int>();

            int pageSize = 10000;
            int page = 0;
            string pageSql = null;
            List<CmsSiteOrderExport> pageList = null;
            int pageCount = (maxID - minID + 1) / pageSize + ((maxID - minID + 1) % pageSize > 0 ? 1 : 0);
            string fileName = @"E:\CMS数据下载\" + siteName + @"数据下载\SqlData_{page}.xlsx";
            string pageFileName = null;
            IWorkbook workbook = null;
            do
            {
                page++;
                this._logger.LogInformation($"正在查询第{page}/{pageCount}页数据...");
                pageFileName = fileName.Replace("{page}", page.ToString());
                if (!System.IO.File.Exists(pageFileName))
                {
                    pageSql = sql.Replace("{wheresql}", $"o.ID>={minID + (page - 1) * pageSize} AND o.ID<{minID + page * pageSize}");
                    pageList = await this._baseRepository.QueryAsync<CmsSiteOrderExport>(EDBConnectionType.SqlServer, pageSql);

                    workbook = this._excelHelper.CreateOrUpdateWorkbook(pageList);
                    this._excelHelper.SaveWorkbookToFile(workbook, pageFileName);
                }
            } while (minID + page * pageSize - 1 < maxID);

            this._logger.LogInformation("下载完成！");
        }

        [Route("SumFileData")]
        [HttpGet]
        public void SumFileData()
        {
            string[] accountDirectoryArray = Directory.GetDirectories(@"E:\CMS数据下载");

            IWorkbook workbook = null;

            foreach (string accountDirectory in accountDirectoryArray)
            {
                List<CmsSiteOrderExport> allDataList = new List<CmsSiteOrderExport>(50 * 10000);
                string[] accountDirectoryFileArray = Directory.GetFiles(accountDirectory);
                foreach (string accountDirectoryFile in accountDirectoryFileArray)
                {
                    this._logger.LogInformation($"正在合并账号文件:{accountDirectoryFile}");
                    List<CmsSiteOrderExport> pageList = this._excelHelper.ReadTitleDataList<CmsSiteOrderExport>(accountDirectoryFile, new ExcelFileDescription());
                    allDataList.AddRange(pageList);
                }
                workbook = this._excelHelper.CreateOrUpdateWorkbook(allDataList);
                this._excelHelper.SaveWorkbookToFile(workbook, @$"{accountDirectory}\AllData.xlsx");
            }

            this._logger.LogInformation("合并完成！");
        }

        #region 临时方法
        [Route("RemoveXuanData")]
        [HttpGet]
        public void RemoveXuanData()
        {
            List<TJ_CmsOrder> allDataList = this._excelHelper.ReadTitleDataList<TJ_CmsOrder>(@"C:\Users\lixianghong\Desktop\CMS刷数据\林总\瑞铭公司补站点_1_去除筛选项.xlsx", new ExcelFileDescription());
            string newFile = @"C:\Users\lixianghong\Desktop\CMS刷数据\林总\瑞铭公司补站点_2_去除轩总站点.xlsx";

            string[] xzSiteArray = new string[] { "SPF4185", "SPF4221", "SPF4222", "SPL6874", "SPL6876", "SPL6880", "SPL6881" };
            string[] tjSiteArray = new string[] { "www.beddinginn.com", "www.oroyalcars.com" };

            foreach (TJ_CmsOrder cmsOrder in allDataList)
            {
                if (xzSiteArray.Contains(cmsOrder.SiteName.Trim()))
                {
                    cmsOrder.Remark = "欢总";
                }
                else if (tjSiteArray.Contains(cmsOrder.SiteName.Trim()))
                {
                    cmsOrder.Remark = "宋姐";
                }
            }
            IWorkbook workbook = this._excelHelper.CreateOrUpdateWorkbook(allDataList);
            this._excelHelper.SaveWorkbookToFile(workbook, newFile);
        }

        #endregion

        [Route("CopyDataToSite")]
        [HttpGet]
        public void CopyDataToSite()
        {
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

            string tjOrderSql = @$"SELECT u.Email UserEmail,
                            (SELECT SUM(ob.BuyCount) FROM dbo.TB_OrderBill ob WHERE ob.SiteID=o.SiteID AND ob.OrderID=o.ID) ProductCount,
                            a.CountryID,
                            o.* FROM dbo.TB_Order o
                            LEFT JOIN dbo.TB_Site s ON s.ID=o.SiteID
                            LEFT JOIN dbo.TB_Users AS u ON o.UserID = U.ID AND o.SiteID = u.SiteID
                            LEFT JOIN dbo.TB_UserSendAddressOrder AS a WITH (NOLOCK) ON o.SiteID = a.SiteID AND a.OrderID = o.ID AND a.AddressID = o.Address1
                            WHERE o.AddTime>'2022-01-01' AND o.AddTime<'2022-07-01' AND o.SiteID NOT IN (0,1,11,1363,18,19,195,27,34,35,36,37,41,43,6689,6874,6876,6880,6881,6916,7162,7163,7003,7143)
                            Order By o.AddTime";
            List<TJ_TB_Order> awaitOrderJObjList = this._baseRepository.QueryAsync<TJ_TB_Order>(EDBConnectionType.SqlServer, tjOrderSql).Result;

            Random random = new Random();
            int orderObjPosition = 0;
            int orderObjTotalCount = awaitOrderJObjList.Count;

            Func<TJ_TB_Order, Task> syncOrderFunc = async (orderObj) =>
            {
                try
                {
                    orderObjPosition++;
                    this._logger.LogInformation($"正在同步1月第{orderObjPosition}/{orderObjTotalCount}个订单...");

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
                            fpOrderID = await this._tjOrderRepository.GetMaxID(fpSiteID) + 1;
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
                    Task syncTask = syncOrderFunc(awaitOrderJObjList[waitObjIndex]);
                    syncTaskList.Add(syncTask);
                    waitObjIndex++;
                }
                //检测异步方法是否全部执行完毕
                allIsSync = syncTaskList.Exists(m => m.IsCompleted == false) ? false : true;
                //移除已完成任务，安排下一对象同步
                syncTaskList.RemoveAll(x => x.IsCompleted == true);
            } while (allIsSync == false || waitObjIndex < awaitOrderJObjList.Count);

            this._logger.LogInformation("订单处理完成！");
        }

    }
}
