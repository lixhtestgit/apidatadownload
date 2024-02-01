using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebApplication1.ExcelCsv;
using WebApplication1.Helper;
using WebApplication1.Model.ExcelModel;
using WebApplication1.Model.MeShop;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 订单地址数据检测控制器
    /// </summary>
    [Route("api/OrderAddress")]
    [ApiController]
    public class OrderAddressController : ControllerBase
    {
        protected HttpClient PayHttpClient;
        public ExcelHelper ExcelHelper;
        public CsvHelper CsvHelper;
        private readonly IWebHostEnvironment WebHostEnvironment;
        public ILogger Logger;

        private MeShopHelper MeShopHelper;

        public OrderAddressController(
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            CsvHelper csvHelper,
            IWebHostEnvironment webHostEnvironment,
            ILogger<OrderShipController> logger,
            MeShopHelper meShopHelper)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.Logger = logger;
            this.ExcelHelper = excelHelper;
            this.CsvHelper = csvHelper;
            this.WebHostEnvironment = webHostEnvironment;
            this.MeShopHelper = meShopHelper;
        }

        /// <summary>
        /// 通过google检测订单发货数据
        /// api/OrderAddress/CheckOrderShipDataByGoogle
        /// </summary>
        /// <returns></returns>
        [Route("CheckOrderShipDataByGoogle")]
        [HttpGet]
        public async Task CheckOrderShipDataByGoogle()
        {
            int execType = 3;

            List<ExcelOrderShipDataCheck> allData = new List<ExcelOrderShipDataCheck>(10000);

            Func<ExcelOrderShipDataCheck, Task> checkAddressAction = async (dataSource) =>
            {
                string validApiUrl = "https://addressvalidation.googleapis.com/v1:validateAddress?key=AIzaSyCVbCFfwi-mNp_5fyG9IOcP3iq5cbEcb-U";

                string validAddress = dataSource.Address1;
                if (!string.IsNullOrWhiteSpace(dataSource.City))
                {
                    validAddress += $",{dataSource.City}";
                }

                string postData = JsonConvert.SerializeObject(new
                {
                    address = new
                    {
                        regionCode = dataSource.CountryCode,
                        addressLines = new List<string> { validAddress }
                    }
                });

                var getResult = await this.PayHttpClient.PostJson(validApiUrl, postData);

                if (getResult.Item1 == System.Net.HttpStatusCode.OK && !string.IsNullOrWhiteSpace(getResult.Item2))
                {
                    JObject getResultJObj = JObject.Parse(getResult.Item2);
                    bool addressComplete = getResultJObj.SelectToken("result.verdict.addressComplete")?.ToObject<bool>() ?? false;

                    if (addressComplete == false)
                    {
                        List<string> missTypeList = getResultJObj.SelectToken("result.address.missingComponentTypes")?.ToObject<List<string>>() ?? new List<string>(0);
                        dataSource.Remark = JsonConvert.SerializeObject(missTypeList);

                        if (missTypeList.Count > 0)
                        {
                            dataSource.Tag = "可疑";
                        }
                        else
                        {
                            dataSource.Tag = "无法确认";
                        }
                    }
                    else
                    {
                        dataSource.Tag = "有效";
                    }
                }
                else if (getResult.Item2.Contains("Unsupported region code"))
                {
                    dataSource.Tag = "不支持";
                    dataSource.Remark = getResult.Item2;
                }
                else
                {
                    dataSource.Tag = "无法确认";
                    dataSource.Remark = getResult.Item2;
                }
            };

            if (execType == 1)
            {
                //从数据库导入csv数据源
                string contentRootPath = this.WebHostEnvironment.ContentRootPath;
                string waitSyncShipDirectoryPath = $@"{contentRootPath}\示例测试目录\订单地址问题检测";
                string[] waitSyncShipFiles = Directory.GetFiles(waitSyncShipDirectoryPath);

                int syncFilePosition = 0;
                int syncTotalFileCount = waitSyncShipFiles.Length;

                foreach (var waitSyncShipFilePath in waitSyncShipFiles)
                {
                    syncFilePosition++;

                    this.Logger.LogInformation($"正在处理第{syncFilePosition}/{syncTotalFileCount}个文件数据...");

                    List<ExcelOrderShipDataCheck> orderShipDataCheckList = this.CsvHelper.Read<ExcelOrderShipDataCheck>(waitSyncShipFilePath, new CsvFileDescription('\t', -1, Encoding.UTF8));

                    int dataTotalCount = orderShipDataCheckList.Count;
                    int dataIndex = 0;
                    foreach (ExcelOrderShipDataCheck excelOrderShipDataCheck in orderShipDataCheckList)
                    {
                        //string apiUrl = searchListApiUrl.Replace("{input}", excelOrderShipDataCheck.Address1).Replace("{countryCode}", excelOrderShipDataCheck.CountryCode);
                        dataIndex++;

                        this.Logger.LogInformation($"正在处理第{syncFilePosition}/{syncTotalFileCount}个文件,第{dataIndex}/{dataTotalCount}个数据...");

                        await checkAddressAction(excelOrderShipDataCheck);
                    }

                    allData.AddRange(orderShipDataCheckList);
                }
            }
            else if (execType == 2)
            {
                //利用整理好的数据二次梳理
                string dataFilePath = @$"C:\Users\lixianghong\Desktop\newFile210844.xlsx";
                List<ExcelOrderShipDataCheck> orderShipDataCheckList = this.ExcelHelper.ReadTitleDataList<ExcelOrderShipDataCheck>(dataFilePath, new ExcelFileDescription());

                int dataTotalCount = orderShipDataCheckList.Count;
                int dataIndex = 0;
                foreach (ExcelOrderShipDataCheck excelOrderShipDataCheck in orderShipDataCheckList)
                {
                    dataIndex++;

                    this.Logger.LogInformation($"正在处理第{dataIndex}/{dataTotalCount}个数据...");

                    if (string.IsNullOrWhiteSpace(excelOrderShipDataCheck.Tag)
                        || excelOrderShipDataCheck.Tag == "忽略"
                        || excelOrderShipDataCheck.Tag == "无法确认"
                        || (excelOrderShipDataCheck.Tag == "可疑" && excelOrderShipDataCheck.Remark == "[]"))
                    {
                        await checkAddressAction(excelOrderShipDataCheck);
                    }
                }

                allData.AddRange(orderShipDataCheckList);
            }
            else if (execType == 3)
            {
                //利用整理好的数据二次梳理
                string dataFilePath = @$"C:\Users\lixianghong\Desktop\newFile212411.xlsx";
                List<ExcelOrderShipDataCheck> orderShipDataCheckList = this.ExcelHelper.ReadTitleDataList<ExcelOrderShipDataCheck>(dataFilePath, new ExcelFileDescription());

                Dictionary<string, List<string>> tagOrderDic = new Dictionary<string, List<string>>(3);
                Dictionary<string, int> tagOrderCountDic = new Dictionary<string, int>(3);
                Dictionary<string, decimal> tagPriceDic = new Dictionary<string, decimal>(3);
                foreach (ExcelOrderShipDataCheck item in orderShipDataCheckList)
                {
                    if (!tagOrderDic.ContainsKey(item.Tag))
                    {
                        tagOrderDic.Add(item.Tag, new List<string>(0));
                    }
                    if (!tagPriceDic.ContainsKey(item.Tag))
                    {
                        tagPriceDic.Add(item.Tag, 0);
                    }
                    if (!tagOrderCountDic.ContainsKey(item.Tag))
                    {
                        tagOrderCountDic.Add(item.Tag, 0);
                    }
                    if (!tagOrderDic[item.Tag].Contains(item.OrderID))
                    {
                        tagOrderDic[item.Tag].Add(item.OrderID);
                        tagOrderCountDic[item.Tag] += 1;
                        tagPriceDic[item.Tag] += (item.TotalUSDPayPrice);
                    }
                }

                string tagOrderCountShow = JsonConvert.SerializeObject(tagOrderCountDic);
                string tagPriceShow = JsonConvert.SerializeObject(tagPriceDic);
                this.Logger.LogInformation(tagOrderCountShow);
                this.Logger.LogInformation(tagPriceShow);
            }

            if (execType != 3)
            {
                //补充多币种汇率，统一为美金
                List<BaseCurrencyModel> baseCurrencyList = await this.MeShopHelper.GetMSCurrency();
                foreach (ExcelOrderShipDataCheck orderShipDataCheck in allData)
                {
                    if (orderShipDataCheck.ChoiseCurrencyRate <= 0)
                    {
                        orderShipDataCheck.ChoiseCurrencyRate = baseCurrencyList.FirstOrDefault(m => m.Name == orderShipDataCheck.ChoiseCurrency).Rate;
                        orderShipDataCheck.TotalUSDPayPrice = Math.Round(orderShipDataCheck.CurrencyTotalPayPrice / orderShipDataCheck.ChoiseCurrencyRate, 2);
                    }
                }

                IWorkbook workbook = this.ExcelHelper.CreateOrUpdateWorkbook(allData);
                this.ExcelHelper.SaveWorkbookToFile(workbook, @$"C:\Users\lixianghong\Desktop\newFile{DateTime.Now.ToString("HHmmss")}.xlsx");

                this.Logger.LogInformation($"同步结束.");
            }
        }
    }
}
