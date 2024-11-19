using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PPPayReportTools.Excel;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
            List<TestCla> aaList = this.ExcelHelper.ReadTitleDataList<TestCla>(@$"C:\Users\lixianghong\Desktop\null.xlsx", new ExcelFileDescription());

            List<string> sqlList = new List<string>();

            var regex = new Regex("[^\\w\\d]");
            var regex1 = new Regex("[-]{2,}");
            foreach (var item in aaList)
            {
                string handle = regex.Replace(item.title, "-").ToLower();
                handle = regex1.Replace(handle, "-");
                sqlList.Add($"update product_spu set handle='{handle}' where spuid='{item.spuid}';");
            }
            string updateSql = string.Join("\n", sqlList);

            await Task.CompletedTask;
        }
    }

    public class TestCla
    {
        [ExcelTitle("spuid")]
        public string spuid { get; set; }
        [ExcelTitle("title")]
        public string title { get; set; }
    }
}
