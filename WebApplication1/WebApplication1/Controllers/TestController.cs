using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Helper;

namespace WebApplication1.Controllers
{
    [Route("api/test")]
    [ApiController]
    public class TestController : ControllerBase
    {
        public ExcelHelper ExcelHelper { get; set; }
        public ImageHelper ImageHelper { get; }
        public IWebHostEnvironment WebHostEnvironment { get; set; }
        public ILogger Logger { get; set; }
        public IConfiguration Configuration { get; set; }

        public TestController(
            ExcelHelper excelHelper,
            ImageHelper imageHelper,
            IWebHostEnvironment webHostEnvironment,
            ILogger<TestController> logger,
            IConfiguration configuration)
        {
            this.ExcelHelper = excelHelper;
            this.ImageHelper = imageHelper;
            this.WebHostEnvironment = webHostEnvironment;
            this.Logger = logger;
            this.Configuration = configuration;
        }

        /// <summary>
        /// excel处理
        /// api/test/Excel
        /// </summary>
        /// <returns></returns>
        [Route("Excel")]
        [HttpGet]
        public async Task<IActionResult> BuildEnJsonToExcel()
        {
            List<ExcelLinShi> dataList = this.ExcelHelper.ReadTitleDataList<ExcelLinShi>(@"C:\Users\lixianghong\Desktop\广州仓库存明细最新20230220(1).xlsx", new ExcelFileDescription());
            dataList = dataList.FindAll(m => m.ImageSrc.IsNotNullOrEmpty()).ToList();
            int i = 0;
            int t = dataList.Count;
            foreach (ExcelLinShi data in dataList)
            {
                i++;
                string imageExtendName = data.ImageSrc.Split('.').LastOrDefault();
                this.Logger.LogInformation($"正在下载第{i}/{t}个图片...");
                await this.ImageHelper.DownLoadImage(data.ImageSrc, $@"C:\Users\lixianghong\Desktop\DownLoad\{data.SKUCode}.{imageExtendName}");
            }

            return Ok();
        }

    }

    public class ExcelLinShi
    {
        [ExcelTitle("库房SKU编码")]
        public string SKUCode { get; set; }
        [ExcelTitle("销售SKUID")]
        public string SKUID { get; set; }
        [ExcelTitle("产品图地址", true)]
        public string ImageSrc { get; set; }
    }
}
