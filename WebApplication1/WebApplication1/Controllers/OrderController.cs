using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.Model;

namespace WebApplication1.Controllers
{
    [Route("api/Order")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        protected HttpClient PayHttpClient { get; set; }
        public ExcelHelper ExcelHelper { get; set; }
        public IWebHostEnvironment WebHostEnvironment { get; set; }
        public ILogger Logger { get; set; }
        public IConfiguration Configuration { get; set; }

        public OrderController(IHttpClientFactory httpClientFactory, ExcelHelper excelHelper, IWebHostEnvironment webHostEnvironment, ILogger<TestController> logger, IConfiguration configuration)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
            this.Logger = logger;
            this.Configuration = configuration;
        }

        /// <summary>
        /// 将enJSON文建转换为EXCEL发给产品进行翻译
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task<IActionResult> BuildEnJsonToExcel()
        {
            string templateName = "Template1";

            IFileProvider fileProvider = this.WebHostEnvironment.ContentRootFileProvider;
            IFileInfo fileInfo = fileProvider.GetFileInfo($"en-{templateName}.json");

            string fileContent = null;
            using (StreamReader readSteam = new StreamReader(fileInfo.CreateReadStream()))
            {
                fileContent = await readSteam.ReadToEndAsync();
            }
            JObject templateFileJObj = JObject.Parse(fileContent);
            JArray pageJPropertyList = templateFileJObj.SelectToken("data.Results").ToObject<JArray>();

            if (pageJPropertyList.Count() == 0)
            {
                throw new Exception($"未找到MyData_{templateName}的配置数据");
            }

            List<Order> dataList = new List<Order>(300);
            foreach (JObject pageJProperty in pageJPropertyList)
            {
                Order model = new Order
                {
                    OrderGuid = pageJProperty.SelectToken("Guid").ToObject<string>(),
                    CreateTime = pageJProperty.SelectToken("CreateTime").ToObject<DateTime>(),
                    IsSended = pageJProperty.SelectToken("IsSended").ToObject<int>() > 0 ? "已发送" : "未发送",
                    UserName = pageJProperty.SelectToken("UserName").ToObject<string>(),
                    State = new int[] { -1, 0, 5, 6 }.Contains(pageJProperty.SelectToken("State").ToObject<int>()) ? "未恢复" : "已恢复",
                    TotalPrice=$"{pageJProperty.SelectToken("ChoiseCurrency")} {pageJProperty.SelectToken("ChoiseCurrencySymbol")} {pageJProperty.SelectToken("CurrencyTotalPayPrice")}",
                };
                dataList.Add(model);
            }

            IWorkbook workbook = ExcelHelper.CreateOrUpdateWorkbook(dataList);

            ExcelHelper.SaveWorkbookToFile(workbook, @"C:\Users\lixianghong\Desktop\Test.xlsx");

            return Ok();
        }
    }
}
