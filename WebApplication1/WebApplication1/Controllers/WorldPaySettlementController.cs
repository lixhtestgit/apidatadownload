using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using WebApplication1.Model.ExcelModel;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// WP结算报告分析
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class WorldPaySettlementController : ControllerBase
    {
        public IWebHostEnvironment WebHostEnvironment;
        protected HttpClient PayHttpClient;
        public ExcelHelper ExcelHelper;
        public ILogger Logger;

        public WorldPaySettlementController(
            IWebHostEnvironment webHostEnvironment,
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            ILogger<WorldPaySettlementController> logger)
        {
            this.WebHostEnvironment = webHostEnvironment;
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.Logger = logger;
        }

        /// <summary>
        /// 获取报告
        /// api/WorldPaySettlement/GetExport
        /// </summary>
        [HttpGet("GetExport")]
        public void GetExport()
        {
            string[] filterOrderTXArray = "r1Z7KTZPKjhW6xCZNTiKkrsSa,r2na44OsljtAeIQ1PKGLxz8fq,r5dEaxukbKI9676fqKfbTPbAJ,r6Zc59BKtWg82nSi8xWKen2So,r7rKK1PhObVkVluP45LiFC5iy,r82crLHMlPKV7tfT2HvM1yQoE,rAo6Lv4QAwF2PoTyW8WQofIPO,rBCgErjlgmV4aHnkZNKs7jBZZ,rCcFVXLyOBanwHHhmeWPX6C3w,rCdQutrOO92kvHzp2H5nPIjNq,rDRgf7YEDHYUCxZT2XPJY7Gih,rDuMYZZkPSMzDt6nZbA6RLWyM,rF4dH0rBVxdWrQ61ZLhG4TZGw,rIT6oXo8DPGkXd2jAdBWZhvyt,rJY0OlnhZPtbhph7vWZESWvxz,rKXyekf7H3P4nL6f94KQk6cDD,rLbwWeYkLsKpHxWm350LNXBpO,rPXGckb6yQ5MQ3mSMPohtXnZC,rQ6PlchUy52eLZV4ndjmvuIse,rQRQip4aytUvu4pvbfDHXDOjG,rQWW55Phufw6kJXhg77LUSyE3,rQxLlR12c4T1LrU7lch8K9jw4,repnlMthZDHLwDtaQeD0dyt83,rgCXyExpJPy1jpGMjMYAVSbEK,rhYKqsDC4sTkErrlXuk0ahSb5,riWHuWTsC41a0HavPZRbLkXbi,riyyqMSnWnwjjtmKmTAGJJTdl,rm9ozGtppJz0NJc69KfgQpnks,rosQnjayM7Jaq1S07BtsAfmVs,rpUYDqS93avnOmBov5frZTM37,rxHGISJCQRsfKmikckg03nHUd,rxQwgbO0lUMeNwnDxw7m2O4Dw,rypTCUgyKLcOZCwJJPxOuUwXb".Split(',');

            List<WorldPaySettlementModel> allSettlementDataList = new List<WorldPaySettlementModel>(1000);

            string dataSourceDirectoryPath = @"C:\Users\lixianghong\Desktop\新建文件夹\New";
            string[] sourceFiles = Directory.GetFiles(dataSourceDirectoryPath);
            foreach (string sourceFile in sourceFiles)
            {
                allSettlementDataList.AddRange(this.ExcelHelper.ReadTitleDataList<WorldPaySettlementModel>(sourceFile, new ExcelFileDescription(7)));
            }

            List<WorldPaySettlementModel> filterSettlementDataList = allSettlementDataList.FindAll(m => filterOrderTXArray.Contains(m.TransactionID));
            IWorkbook workbook = this.ExcelHelper.CreateOrUpdateWorkbook(filterSettlementDataList);
            this.ExcelHelper.SaveWorkbookToFile(workbook, dataSourceDirectoryPath + @"\汇总.xlsx");
        }

        /// <summary>
        /// 匹配网站交易
        /// api/WorldPaySettlement/MatchWebsite
        /// </summary>
        [HttpGet("MatchWebsite")]
        public void MatchWebsite()
        {
            string contentRootPath = this.WebHostEnvironment.ContentRootPath;

            //收集支付公司交易信息
            string websiteSourceFilePath = @$"{contentRootPath}\示例测试目录\支付公司导出订单\WP9月交易.xlsx";
            List<WorldPayTranscationModel> websiteTranList = this.ExcelHelper.ReadTitleDataList<WorldPayTranscationModel>(websiteSourceFilePath, new ExcelFileDescription(0));

            //收集网站交易信息
            Dictionary<string, List<WorldPayWebsiteTranscationModel>> webTranDic = new Dictionary<string, List<WorldPayWebsiteTranscationModel>>();
            string payCompanyTranFilePath = @$"{contentRootPath}\示例测试目录\支付公司导出订单\WP网站9月1-9月30日.xlsx";
            IWorkbook payCompanyTranWorkbook = this.ExcelHelper.CreateWorkbook(payCompanyTranFilePath);
            List<ISheet> payCompanyTranSheetList = this.ExcelHelper.GetSheetList(payCompanyTranWorkbook);
            foreach (ISheet payCompanyTranSheet in payCompanyTranSheetList)
            {
                List<WorldPayWebsiteTranscationModel> webTranList = this.ExcelHelper.ReadTitleDataList<WorldPayWebsiteTranscationModel>(payCompanyTranSheet, new ExcelFileDescription(0));
                webTranDic.Add(payCompanyTranSheet.SheetName, webTranList);
            }

            //匹配关系
            foreach (WorldPayTranscationModel item in websiteTranList)
            {
                foreach (var webTranItem in webTranDic)
                {
                    WorldPayWebsiteTranscationModel worldPayWebsiteTranscation = webTranItem.Value.FirstOrDefault(m => (m.Tx?.Trim().ToLower() ?? "1") == (item.OrderCode?.Trim().ToLower() ?? "2"));
                    if (worldPayWebsiteTranscation != null)
                    {
                        item.Website = webTranItem.Key;
                        break;
                    }
                }
            }

            IWorkbook newWorkbook = this.ExcelHelper.CreateOrUpdateWorkbook(websiteTranList);
            this.ExcelHelper.SaveWorkbookToFile(newWorkbook, @$"C:\Users\lixianghong\Desktop\WP9月交易匹配结果.xlsx");
        }
    }
}
