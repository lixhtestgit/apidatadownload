using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication1.ExcelCsv;
using WebApplication1.Model.CsvModel;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// CountryZIP控制器
    /// </summary>
    [Route("api/CountryZIP")]
    [ApiController]
    public class CountryZIPController : ControllerBase
    {
        public IWebHostEnvironment WebHostEnvironment;
        public ExcelHelper ExcelHelper;
        public CsvHelper CsvHelper;
        public ILogger Logger;

        public CountryZIPController(
            IWebHostEnvironment webHostEnvironment,
            ExcelHelper excelHelper,
            CsvHelper csvHelper,
            ILogger<MeShopSpuController> logger)
        {
            this.WebHostEnvironment = webHostEnvironment;
            this.ExcelHelper = excelHelper;
            this.CsvHelper = csvHelper;
            this.Logger = logger;
        }

        /// <summary>
        /// 修改产品状态
        /// api/CountryZIP/GetZip
        /// </summary>
        /// <returns></returns>
        [Route("GetZip")]
        [HttpGet]
        public IActionResult GetZip()
        {
            string dataSourceFilePath = $@"{this.WebHostEnvironment.ContentRootPath}\示例测试目录\国家邮编\uszips.csv";
            List<CsvCountryZIP> countryZIPList = this.CsvHelper.Read<CsvCountryZIP>(dataSourceFilePath, new CsvFileDescription());

            List<CountryZIPData> provinceList = new List<CountryZIPData>(100);

            CountryZIPData countryZIPData = null;
            CountryZIPData_ZIP countryZIPData_ZIP = null;
            foreach (CsvCountryZIP countryZIP in countryZIPList)
            {
                countryZIPData = provinceList.FirstOrDefault(m => m.ProvinceCode == countryZIP.ProvinceCode);
                if (countryZIPData == null)
                {
                    countryZIPData = new CountryZIPData
                    {
                        ProvinceCode = countryZIP.ProvinceCode,
                        ProvinceName = countryZIP.ProvinceName,
                        ZipList = new List<CountryZIPData_ZIP>(0)
                    };
                    provinceList.Add(countryZIPData);
                }
                countryZIPData_ZIP = countryZIPData.ZipList.FirstOrDefault(m => (Convert.ToInt32(m.MinZip) - 1) <= Convert.ToInt32(countryZIP.ZIP.Substring(0, 3)) && (Convert.ToInt32(m.MaxZip) + 1) >= Convert.ToInt32(countryZIP.ZIP.Substring(0, 3)));
                if (countryZIPData_ZIP == null)
                {
                    countryZIPData_ZIP = new CountryZIPData_ZIP
                    {
                        StartFlag = "",
                        MinZip = countryZIP.ZIP.Substring(0, 3),
                        MaxZip = countryZIP.ZIP.Substring(0, 3)
                    };
                    countryZIPData.ZipList.Add(countryZIPData_ZIP);
                }
                else
                {
                    if (Convert.ToInt32(countryZIP.ZIP.Substring(0, 3)) < Convert.ToInt32(countryZIPData_ZIP.MinZip))
                    {
                        countryZIPData_ZIP.MinZip = countryZIP.ZIP.Substring(0, 3);
                    }
                    else if (Convert.ToInt32(countryZIP.ZIP.Substring(0, 3)) > Convert.ToInt32(countryZIPData_ZIP.MaxZip))
                    {
                        countryZIPData_ZIP.MaxZip = countryZIP.ZIP.Substring(0, 3);
                    }
                }
            }

            string provinceDicStr = JsonConvert.SerializeObject(provinceList);

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }
    }
}
