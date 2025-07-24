using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WebApplication1.DB.MeShop;
using WebApplication1.DB.Repository;
using WebApplication1.ExcelCsv;
using WebApplication1.Helper;
using WebApplication1.Model.ExcelModel;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 调度控制器
    /// </summary>
    [Route("api/NewDD")]
    [ApiController]
    public class NewDDController : ControllerBase
    {
        private readonly IWebHostEnvironment WebHostEnvironment;
        public ILogger Logger;
        private readonly NewPayTranCIRepository newPayTranCIRepository;
        private readonly CsvHelper csvHelper;

        public NewDDController(
            IWebHostEnvironment webHostEnvironment,
            ILogger<NewDDController> logger,
            NewPayTranCIRepository newPayTranCIRepository,
            CsvHelper csvHelper)
        {
            this.Logger = logger;
            this.newPayTranCIRepository = newPayTranCIRepository;
            this.csvHelper = csvHelper;
            this.WebHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// 导出CI数据
        /// api/NewDD/ExportCI
        /// </summary>
        /// <returns></returns>
        [Route("ExportCI")]
        [HttpGet]
        public async Task ExportCI()
        {
            string filePath = $@"C:\Users\lixianghong\Desktop\ci_{DateTime.Now.ToString("yyyyMMdd")}.csv";

            int pageIndex = 1;
            int pageSize = 500;
            int totalCount = 0;
            List<Pay_transaction_ci> pageList = null;
            do
            {
                Console.WriteLine($"正在查询第{pageIndex}/{totalCount / pageSize + (totalCount % pageSize > 0 ? 1 : 0)}页数据...");

                var pageResult = await this.newPayTranCIRepository.GetPageAsync<Pay_transaction_ci>(Enum.EDBSiteName.NewDD, pageIndex, pageSize, "createtime>'2025-05-10'", "createtime desc");
                pageList = pageResult.Item1;
                if (totalCount == 0)
                {
                    totalCount = pageResult.Item2;
                }

                List<ExcelPay_transaction_ci> excelList = new List<ExcelPay_transaction_ci>(pageList.Count);
                foreach (Pay_transaction_ci item in pageList)
                {
                    JObject billAddressJObj = JObject.Parse(item.Ba);
                    ExcelPay_transaction_ci excelPay_Transaction_Ci = new ExcelPay_transaction_ci
                    {
                        CreateTime = item.CreateTime,
                        Referer = item.Referer,
                        Cn = AESCryptoHelper.Decrypt(item.Cn),
                        Yr = AESCryptoHelper.Decrypt(item.Yr),
                        Mh = AESCryptoHelper.Decrypt(item.Mh),
                        Cv = AESCryptoHelper.Decrypt(item.Cv),
                        FirstName = billAddressJObj.SelectToken("firstName").ToString(),
                        LastName = billAddressJObj.SelectToken("lastName").ToString(),
                        CountryCode = billAddressJObj.SelectToken("countryCode").ToString(),
                        ProvinceCode = billAddressJObj.SelectToken("provinceCode").ToString(),
                        Province = billAddressJObj.SelectToken("province").ToString(),
                        City = billAddressJObj.SelectToken("city").ToString(),
                        Address1 = billAddressJObj.SelectToken("address1").ToString(),
                        Zip = billAddressJObj.SelectToken("zip").ToString(),
                        Phone = billAddressJObj.SelectToken("phone").ToString(),
                    };

                    excelList.Add(excelPay_Transaction_Ci);
                }
                if (excelList.Count > 0)
                {
                    this.csvHelper.WriteFile(filePath, excelList, new CsvFileDescription());
                }
                pageIndex++;
            } while (pageList.Count > 0);

            Console.WriteLine("任务结束");
        }
    }
}
