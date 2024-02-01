using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PPPayReportTools.Excel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 订单地址数据检测控制器
    /// </summary>
    [Route("api/Test")]
    [ApiController]
    public class TestController : ControllerBase
    {
        public ExcelHelper ExcelHelper;
        private readonly IWebHostEnvironment WebHostEnvironment;
        public ILogger Logger;

        public TestController(
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            ILogger<OrderShipController> logger)
        {
            this.Logger = logger;
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// api/Test/
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task Test()
        {
            List<TestCla> aaList = this.ExcelHelper.ReadTitleDataList<TestCla>(@$"C:\Users\lixianghong\Desktop\新建 XLSX 工作表.xlsx", new ExcelFileDescription());

            string dataStr = JsonConvert.SerializeObject(aaList);

            await Task.CompletedTask;
        }
    }

    public class TestCla
    {
        [ExcelTitle("CountryName")]
        public string CountryName { get; set; }
        [ExcelTitle("CountryCode")]
        public string CountryCode { get; set; }
        [ExcelTitle("CountryCode3")]
        public string CountryCode3 { get; set; }
        [ExcelTitle("CountryNumber1")]
        [JsonIgnore]
        public int CountryNumber1 { get; set; }
        [ExcelTitle("CountryNumber")]
        public string CountryNumber
        {
            get
            {
                return this.CountryNumber1.ToString("000");
            }
        }
    }
}
