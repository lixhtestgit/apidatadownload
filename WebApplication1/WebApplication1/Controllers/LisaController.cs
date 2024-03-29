﻿using Microsoft.AspNetCore.Hosting;
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
            //1-获取数据源
            List<ExcelOrderShipData_Lisa> orderShipDataCheckList = new List<ExcelOrderShipData_Lisa>();
            string dataDicPath = @$"E:\公司小项目\弃单支付方式查询\apidatadownload\WebApplication1\WebApplication1\示例测试目录\lisa发货订单\项总_beatyeyes\数据源";
            string[] waitSyncShipFiles = Directory.GetFiles(dataDicPath);
            foreach (string file in waitSyncShipFiles)
            {
                orderShipDataCheckList.AddRange(this.ExcelHelper.ReadTitleDataList<ExcelOrderShipData_Lisa>(file, new ExcelFileDescription()));
            }

            //2-获取已导出数据
            List<ExcelOrderShipData_Lisa> hadUsedOrderShipDataCheckList = new List<ExcelOrderShipData_Lisa>();
            dataDicPath = @$"E:\公司小项目\弃单支付方式查询\apidatadownload\WebApplication1\WebApplication1\示例测试目录\lisa发货订单\项总_beatyeyes\导出数据";
            waitSyncShipFiles = Directory.GetFiles(dataDicPath);
            foreach (string file in waitSyncShipFiles)
            {
                hadUsedOrderShipDataCheckList.AddRange(this.ExcelHelper.ReadTitleDataList<ExcelOrderShipData_Lisa>(file, new ExcelFileDescription()));
            }

            //3-移除已导出数据（注意上面的数据必须在同一店铺下）
            List<string> hadUsedOrderIDList = hadUsedOrderShipDataCheckList.Select(m => m.OrderID).ToList();
            orderShipDataCheckList.RemoveAll(m => hadUsedOrderIDList.Contains(m.OrderID));


            decimal limitSumTotalPrice = 50200;
            decimal sumTotalPrice = orderShipDataCheckList.Sum(m => m.TotalPayPrice);

            Random random = new Random();

            while (sumTotalPrice > limitSumTotalPrice)
            {
                int ranIndex = random.Next(0, orderShipDataCheckList.Count);

                sumTotalPrice -= orderShipDataCheckList[ranIndex].TotalPayPrice;
                orderShipDataCheckList.RemoveAt(ranIndex);
            }

            orderShipDataCheckList = orderShipDataCheckList.OrderBy(m => m.PayTime).ToList();
            IWorkbook workBook = this.ExcelHelper.CreateOrUpdateWorkbook(orderShipDataCheckList);
            this.ExcelHelper.SaveWorkbookToFile(workBook, @$"E:\公司小项目\弃单支付方式查询\apidatadownload\WebApplication1\WebApplication1\示例测试目录\lisa发货订单\项总_beatyeyes\导出数据\项总_lisa提交支付公司订单发货记录_{DateTime.Now.ToString("HHmmss")}.xlsx");

            await Task.CompletedTask;
        }
    }
}
