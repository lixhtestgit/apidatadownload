using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    /// lisa业务控制器
    /// </summary>
    [Route("api/Lisa")]
    [ApiController]
    public class LisaController : ControllerBase
    {
        protected HttpClient PayHttpClient;
        public ExcelHelper ExcelHelper;
        private readonly IWebHostEnvironment WebHostEnvironment;
        public ILogger Logger;

        public LisaController(
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            ILogger<OrderShipController> logger)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.Logger = logger;
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// 订单发货数据过滤
        /// api/Lisa/OrderShipFilter
        /// </summary>
        /// <returns></returns>
        [Route("OrderShipFilter")]
        [HttpGet]
        public async Task OrderShipFilter()
        {
            //利用整理好的数据二次梳理
            string dataDicPath = @$"C:\Users\lixianghong\Downloads\项总_beatyeyes_9月到24年1月";
            string[] waitSyncShipFiles = Directory.GetFiles(dataDicPath);

            List<ExcelOrderShipData_Lisa> orderShipDataCheckList = new List<ExcelOrderShipData_Lisa>();
            foreach (string file in waitSyncShipFiles)
            {
                orderShipDataCheckList.AddRange(this.ExcelHelper.ReadTitleDataList<ExcelOrderShipData_Lisa>(file, new ExcelFileDescription()));
            }

            decimal limitSumTotalPrice = 50200;
            decimal sumTotalPrice = orderShipDataCheckList.Sum(m => m.TotalPayPrice);

            Random random = new Random();

            while (sumTotalPrice > limitSumTotalPrice)
            {
                int ranIndex = random.Next(0, orderShipDataCheckList.Count);

                sumTotalPrice -= orderShipDataCheckList[ranIndex].TotalPayPrice;
                orderShipDataCheckList.RemoveAt(ranIndex);
            }

            IWorkbook workBook = this.ExcelHelper.CreateOrUpdateWorkbook(orderShipDataCheckList);
            this.ExcelHelper.SaveWorkbookToFile(workBook, @$"C:\Users\lixianghong\Downloads\项总_lisa提交支付公司订单发货记录_{DateTime.Now.ToString("HHmmss")}.xlsx");

            await Task.CompletedTask;
        }
    }
}
