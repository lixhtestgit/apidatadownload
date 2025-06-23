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
using WebApplication1.ExcelCsv;
using WebApplication1.Extension;
using WebApplication1.Helper;
using WebApplication1.Model;
using WebApplication1.Model.MeShopNew;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// MeShopV2
    /// </summary>
    [Route("api/MeShopV2")]
    [ApiController]
    public class MeShopV2Controller : ControllerBase
    {
        public IWebHostEnvironment WebHostEnvironment;
        protected HttpClient PayHttpClient;
        public CsvHelper CsvHelper;
        private readonly MeShopNewHelper meShopNewHelper;
        public ILogger Logger;

        public MeShopV2Controller(
            IWebHostEnvironment webHostEnvironment,
            IHttpClientFactory httpClientFactory,
            CsvHelper csvHelper,
            MeShopNewHelper meShopNewHelper,
            ILogger<MeShopCheckoutController> logger)
        {
            this.WebHostEnvironment = webHostEnvironment;
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.CsvHelper = csvHelper;
            this.meShopNewHelper = meShopNewHelper;
            this.Logger = logger;
        }

        /// <summary>
        /// 同步用户和产品
        /// api/MeShopV2/SyncUserAndProduct
        /// </summary>
        /// <returns></returns>
        [Route("SyncUserAndProduct")]
        [HttpGet]
        public async Task<IActionResult> SyncUserAndProduct()
        {
            IDbConnection commonDBConnection = MySqlConnector.MySqlConnectorFactory.Instance.CreateConnection();
            commonDBConnection.ConnectionString = $"Data Source=10.10.0.37;User Id=meshop;Password=rbwv0Uun3vnKEUvAAbPw;Port=3306;default command timeout=100;Connection Timeout=30;Charset=utf8mb4;Allow User Variables=true;IgnoreCommandTransaction=true;";

            string querySQL = "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME LIKE 'shop_%' OR SCHEMA_NAME LIKE 'meshop_shop_%'";
            List<string> databaseList = (await commonDBConnection.QueryAsync<string>(querySQL, null, commandTimeout: 120)).ToList();

            List<SyncUserProduct> allList = new List<SyncUserProduct>(100000);

            int execHostAdminCount = 0;
            int totalCount = databaseList.Count;
            foreach (string database in databaseList)
            {
                execHostAdminCount++;
                this.Logger.LogInformation($"正在查询第{execHostAdminCount}/{totalCount}个店铺用户产品数据...");

                commonDBConnection.ConnectionString = $"Data Source=10.10.0.37;User Id=meshop;Password=rbwv0Uun3vnKEUvAAbPw;Port=3306;default command timeout=100;Connection Timeout=30;Charset=utf8mb4;Allow User Variables=true;IgnoreCommandTransaction=true;database={database}";
                querySQL = @"SELECT MIN(u.CreateTime) CreateTime,
                        u.Email,
                        MIN(a.FirstName) FirstName,
                        MIN(a.LastName) LastName,
                        GROUP_CONCAT(i.Title) ProductTitles,
                        '' as ProductImages,
                        MIN(a.CountryCode) CountryCode
                        from user_info u
                        LEFT JOIN order_master o ON u.ID=o.UserID
                        LEFT JOIN order_item i on i.OrderID=o.ID
                        LEFT JOIN order_address a on a.OrderID=o.ID and a.Type=1
                        GROUP BY u.Email";
                List<SyncUserProduct> hostAdminDataList = null;
                try
                {
                    hostAdminDataList = (await commonDBConnection.QueryAsync<SyncUserProduct>(querySQL, null, commandTimeout: 120)).ToList();
                }
                catch (Exception e)
                {
                    this.Logger.LogWarning($"查询异常:{e.Message}");
                }
                if (hostAdminDataList != null && hostAdminDataList.Count > 0)
                {
                    allList.AddRange(hostAdminDataList);
                    //break;
                }
            }

            try
            {
                string filePath = @$"C:\Users\lixianghong\Desktop\SyncUserAndProduct_{DateTime.Now.ToString("HHmmss")}.csv";
                this.CsvHelper.WriteFile(filePath, allList, new CsvFileDescription());
            }
            catch (Exception e)
            {
                //throw;
            }

            return Ok();
        }

        #region 扩展类

        private class SyncUserProduct
        {
            [CsvColumn("购买时间")]
            public DateTime CreateTime { get; set; }
            [CsvColumn("邮箱")]
            public string Email { get; set; }
            [CsvColumn("名")]
            public string FirstName { get; set; }
            [CsvColumn("姓")]
            public string LastName { get; set; }
            [CsvColumn("产品标题")]
            public string ProductTitles { get; set; }
            [CsvColumn("产品图片")]
            public string ProductImages { get; set; }
            [CsvColumn("国家编码")]
            public string CountryCode { get; set; }
        }
        #endregion
    }
}
