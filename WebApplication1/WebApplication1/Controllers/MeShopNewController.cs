using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
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

            string insertSql = $"insert into base_permission(ID,ParentID,Type,Name,Icon,Href,IsEnable,Sort,CreateTime) Values {string.Join(',',insertSqlList)};";
            await this.meShopNewHelper.ExecSqlToShop("meshop001", 0, insertSql);

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }
    }
}
