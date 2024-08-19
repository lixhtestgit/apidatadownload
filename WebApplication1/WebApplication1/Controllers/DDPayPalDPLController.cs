using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyPaymentHelper.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.Model.ExcelModel;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// PayPal大批量临时账号配置-控制器
    /// </summary>
    [Route("api/DDPayPalDPL")]
    [ApiController]
    public class DDPayPalDPLController : ControllerBase
    {
        protected HttpClient PayHttpClient;
        public ExcelHelper ExcelHelper;
        private readonly IWebHostEnvironment WebHostEnvironment;
        private readonly DDHelper ddHelper;
        public ILogger Logger;
        public string jq = "mystoresz.com";
        public string clusterCode = "mystoresz.com";
        public string payType = "PayPal";
        //B站域名：mslooyy.xyz      
        //nshopaa.meshopstore.com/admin     chenfei@meshop.net  Mn%2389gh   148.153.49.54
        public string bPayId = "165064be-33a9-4a60-bf09-dc1fcfd1bcbb";
        //A站：https://watchesss.mystoresz.com/admin/home     bikaidan888@163.com    @8CqSV%V%mAF+89X
        public string aSiteCode = "watchesss";

        public DDPayPalDPLController(
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            DDHelper ddHelper,
            ILogger<DDPayPalDPLController> logger)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.Logger = logger;
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
            this.ddHelper = ddHelper;
        }

        /// <summary>
        /// 导入账号
        /// api/DDPayPalDPL/ImportAccount
        /// </summary>
        /// <returns></returns>
        [Route("ImportAccount")]
        [HttpGet]
        public async Task ImportAccount()
        {
            //1-获取数据源
            string dataPayPalAccountPath = @$"{this.WebHostEnvironment.ContentRootPath}\示例测试目录\大批量PayPal临时账号\20240819--25.xls";
            List<ExcelPayPalAccount> fileDataList = this.ExcelHelper.ReadTitleDataList<ExcelPayPalAccount>(dataPayPalAccountPath, new ExcelFileDescription());

            fileDataList.Add(new ExcelPayPalAccount
            {
                Email = "wushan@paypay.com",
                ClientID = "xxx",
                ClientSecret = "xxx"
            });

            string accountName = null;
            string accountConfig = null;
            int accountIndex = 0;
            foreach (ExcelPayPalAccount account in fileDataList)
            {
                accountIndex++;
                accountName = $"linshi_{account.Email}";
                accountConfig = JsonConvert.SerializeObject(new
                {
                    IsSandBox = false,
                    MerchantCode = account.Email,
                    ClientID = account.ClientID,
                    ClientSecret = account.ClientSecret,
                    IsAB = true
                });

                await this.ddHelper.CheckOrAddPayAccount(jq, payType, accountName, accountConfig);
                this.Logger.LogInformation($"已添加第{accountIndex}/{fileDataList.Count}个账号...");
            }
        }

        /// <summary>
        /// 导入规则
        /// api/DDPayPalDPL/ImportAccountRule
        /// </summary>
        /// <returns></returns>
        [Route("ImportAccountRule")]
        [HttpGet]
        public async Task ImportAccountRule()
        {
            List<DDPayAccount> accountList = await this.ddHelper.GetPayAccountListAsync(jq);
            accountList = accountList.FindAll(m => m.Name.StartsWith("linshi_"));

            //调整账号："wushan@paypay.com 到最后一个位置
            int testAccountIndex = accountList.FindIndex(m => m.Name == $"linshi_wushan@paypay.com");
            DDPayAccount testAccount = JObject.FromObject(accountList[testAccountIndex]).ToObject<DDPayAccount>();
            accountList.RemoveAt(testAccountIndex);
            accountList.Add(testAccount);

            List<string> accountIdList = accountList.Select(m => m.Id).ToList();

            await this.ddHelper.AddNumberTransactionsRuleDic(jq, clusterCode, bPayId, payType, aSiteCode, accountIdList);

            this.Logger.LogInformation($"已添加站点规则...");
        }

        /// <summary>
        /// 清理所有临时账号和规则
        /// api/DDPayPalDPL/DeleteAllAccountRule
        /// </summary>
        /// <returns></returns>
        [Route("DeleteAllAccountRule")]
        [HttpGet]
        public async Task DeleteAllAccountRule()
        {
            List<DDPayAccount> accountList = await this.ddHelper.GetPayAccountListAsync(jq);
            accountList = accountList.FindAll(m => m.Name.StartsWith("linshi_"));

            List<string> accountIdList = accountList.Select(m => m.Id).ToList();

            await this.ddHelper.DelPayAccountAsync(jq, accountIdList.ToArray());

            Dictionary<string, List<DDRuleList>> siteRuleDic = await this.ddHelper.GetRuleDic(jq, "siteCode");
            string deleteRuleId = siteRuleDic[aSiteCode].First().Rule.Id;

            await this.ddHelper.DelRuleDic(jq, deleteRuleId);

            this.Logger.LogInformation($"已删除站点规则和账号...");
        }
    }
}
