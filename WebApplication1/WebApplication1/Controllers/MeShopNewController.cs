using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.Extension;
using WebApplication1.Helper;
using WebApplication1.Model;
using WebApplication1.Model.MeShopNew;

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
        public ILogger Logger;

        public MeShopNewController(
            IWebHostEnvironment webHostEnvironment,
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            MeShopNewHelper meShopNewHelper,
            ILogger<MeShopCheckoutController> logger)
        {
            this.WebHostEnvironment = webHostEnvironment;
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.meShopNewHelper = meShopNewHelper;
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
