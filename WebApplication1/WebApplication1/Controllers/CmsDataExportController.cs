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

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CmsDataExportController : ControllerBase
    {
        private BaseRepository _baseRepository;
        private TB_UsersRepository _usersRepository;
        private TB_OrderRepository _orderRepository;
        private TB_OrderBillRepository _orderBillRepository;
        private TB_UserSendAddressOrderRepository _userSendAddressOrderRepository;
        private ExcelHelper _excelHelper;
        private ILogger _logger;

        public CmsDataExportController(
            BaseRepository baseRepository,
            TB_UsersRepository usersRepository,
            TB_OrderRepository orderRepository,
            TB_OrderBillRepository orderBillRepository,
            TB_UserSendAddressOrderRepository userSendAddressOrderRepository,
            ExcelHelper excelHelper,
            ILogger<TestController> logger)
        {
            this._baseRepository = baseRepository;
            this._usersRepository = usersRepository;
            this._orderRepository = orderRepository;
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


        [Route("CopyDataToSite")]
        [HttpGet]
        public async Task CopyDataToSite()
        {
            Dictionary<int, int> siteZB = new Dictionary<int, int>
            {
                {6546,26 },
                {6938,24 },
                {6903,18 },
                {6691,18 },
                {7027,6 },
                {7207,2 },
                {7211,1 },
                {6738,1 },
                {6983,1 },
                {7204,1 },
                {7203,1 },
                {7224,1 }
            };

            string idSql = @$"SELECT o.ID,o.SiteID,u.Email UserEmail 
                            FROM dbo.TB_Order o
                            INNER JOIN dbo.TB_Users u ON u.ID=o.UserID AND u.SiteID=o.SiteID
                            WHERE o.AddTime>='2022-01-01' AND o.AddTime<'2022-07-01'
                            AND o.SiteID NOT IN (6546,6691,6738,6903,6938,6983,7027,7203,7204,7207,7211,7224)";
            List<object> awaitOrderJObjList = await this._baseRepository.QueryAsync<object>(EDBConnectionType.SqlServer, idSql);

            Random random = new Random();
            int orderObjPosition = 0;
            int orderObjTotalCount = awaitOrderJObjList.Count;

            List<TB_Users> insertUserList = new List<TB_Users>(10 * 10000);
            List<TB_OrderBill> insertOrderBillList = new List<TB_OrderBill>(10 * 10000);
            List<TB_UserSendAddressOrder> insertOrderAddressList = new List<TB_UserSendAddressOrder>(10 * 10000);
            List<TB_Order> insertOrderList = new List<TB_Order>(10 * 10000);

            foreach (object orderObj in awaitOrderJObjList)
            {
                try
                {
                    orderObjPosition++;
                    this._logger.LogInformation($"正在同步第{orderObjPosition}/{orderObjTotalCount}个订单...");
                    JObject orderJObj = JObject.FromObject(orderObj);
                    int siteID = orderJObj.SelectToken("SiteID").ToObject<int>();
                    int orderID = orderJObj.SelectToken("ID").ToObject<int>();
                    string userEmail = orderJObj.SelectToken("UserEmail").ToObject<string>();

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
                    //添加用户
                    int fpUserID = 0;

                    TB_Users lsUser = await this._usersRepository.GetModelByEmail(siteID, userEmail);
                    fpUserID = lsUser.ID;

                    TB_Users fpUser = lsUser.Clone();
                    fpUser.ID = fpUserID;
                    fpUser.SiteID = fpSiteID;
                    if (fpUser.RegIP.IsNullOrEmpty())
                    {
                        fpUser.RegIP = "127.0.0.1";
                    }
                    if (fpUser.Password.IsNullOrEmpty())
                    {
                        fpUser.Password = "127.0.0.1";
                    }
                    fpUser.InviteCode = "linshi_lixh";
                    try
                    {
                        await this._usersRepository.Insert(EDBConnectionType.SqlServer, fpUser);
                    }
                    catch (Exception)
                    {
                        fpUserID = await this._usersRepository.GetMaxID(fpSiteID) + 1;
                        fpUser.ID = fpUserID;
                        await this._usersRepository.Insert(EDBConnectionType.SqlServer, fpUser);
                    }
                    insertUserList.Add(fpUser);

                    //添加订单
                    int fpOrderID = 0;
                    TB_Order lsOrder = await this._orderRepository.GetModelByID(siteID, orderID);
                    fpOrderID = lsOrder.ID;
                    TB_Order fpOrder = lsOrder.Clone();
                    fpOrder.ID = fpOrderID;
                    fpOrder.SiteID = fpSiteID;
                    fpOrder.UserID = fpUserID;
                    fpOrder.tag = "linshi_lixh";
                    try
                    {
                        await this._orderRepository.Insert(EDBConnectionType.SqlServer, fpOrder);
                    }
                    catch (Exception)
                    {
                        fpOrderID = await this._orderRepository.GetMaxID(fpSiteID) + 1;
                        fpOrder.ID = fpOrderID;
                        await this._orderRepository.Insert(EDBConnectionType.SqlServer, fpOrder);
                    }
                    insertOrderList.Add(fpOrder);

                    //添加子单
                    List<TB_OrderBill> lsOrderBillList = await this._orderBillRepository.GetModelByOrderID(siteID, orderID);
                    foreach (TB_OrderBill lsOrderBill in lsOrderBillList)
                    {
                        TB_OrderBill fpOrderBill = lsOrderBill.Clone();
                        fpOrderBill.OrderID = fpOrderID;
                        fpOrderBill.SiteID = fpSiteID;
                        fpOrderBill.Remark = "linshi_lixh";
                        try
                        {
                            await this._orderBillRepository.Insert(EDBConnectionType.SqlServer, fpOrderBill);
                        }
                        catch (Exception)
                        {
                            fpOrderBill.ID = await this._orderBillRepository.GetMaxID(fpSiteID) + 1;
                            await this._orderBillRepository.Insert(EDBConnectionType.SqlServer, fpOrderBill);
                        }

                        insertOrderBillList.Add(fpOrderBill);
                    }

                    //添加订单地址
                    List<TB_UserSendAddressOrder> lsOrderAddressList = await this._userSendAddressOrderRepository.GetModelByOrderID(siteID, orderID);
                    foreach (TB_UserSendAddressOrder lsOrderAddress in lsOrderAddressList)
                    {
                        TB_UserSendAddressOrder fpOrderAddress = lsOrderAddress.Clone();
                        fpOrderAddress.OrderID = fpOrderID;
                        fpOrderAddress.SiteID = fpSiteID;
                        fpOrderAddress.AddRessUserName = "linshi_lixh";
                        await this._userSendAddressOrderRepository.Insert(EDBConnectionType.SqlServer, fpOrderAddress);
                        insertOrderAddressList.Add(fpOrderAddress);
                    }
                }
                catch (Exception e)
                {
                    //报错则清理所有已插入数据
                    foreach (var item in insertUserList)
                    {
                        await this._usersRepository.Delete(EDBConnectionType.SqlServer, m => m.SiteID == item.SiteID && m.ID == item.ID);
                    }
                    foreach (var item in insertOrderList)
                    {
                        await this._orderRepository.Delete(EDBConnectionType.SqlServer, m => m.SiteID == item.SiteID && m.ID == item.ID);
                    }
                    foreach (var item in insertOrderBillList)
                    {
                        await this._orderBillRepository.Delete(EDBConnectionType.SqlServer, m => m.SiteID == item.SiteID && m.OrderID == item.OrderID);
                    }
                    foreach (var item in insertOrderAddressList)
                    {
                        await this._userSendAddressOrderRepository.Delete(EDBConnectionType.SqlServer, m => m.SiteID == item.SiteID && m.OrderID == item.OrderID);
                    }

                    throw;
                }
            }

            this._logger.LogInformation("订单处理完成！");
        }
    }
}
