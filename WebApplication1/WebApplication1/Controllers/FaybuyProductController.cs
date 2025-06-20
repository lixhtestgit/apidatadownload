using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql.Internal.TypeHandlers;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.Helper;
using WebApplication1.Model.ExcelModel;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 反向代购产品控制器
    /// </summary>
    [Route("api/FaybuyProduct")]
    [ApiController]
    public class FaybuyProductController : ControllerBase
    {
        public ExcelHelper ExcelHelper;
        private readonly HttpClient httpClient;
        private readonly IWebHostEnvironment WebHostEnvironment;
        public ILogger Logger;

        public FaybuyProductController(
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            IHttpClientFactory httpClientFactory,
            ILogger<OrderShipController> logger)
        {
            this.Logger = logger;
            this.ExcelHelper = excelHelper;
            this.httpClient = httpClientFactory.CreateClient();
            this.WebHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// api/FaybuyProduct/
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task Test()
        {
            List<ExcelFayBuyProduct> aaList = this.ExcelHelper.ReadTitleDataList<ExcelFayBuyProduct>(@$"C:\Users\lixianghong\Desktop\allchinafinds.xlsx", new ExcelFileDescription());

            int lastValidDataCount, validDataCount = 0;
            int currentExecCount = 1;
            do
            {
                Console.WriteLine($"正在执行第{currentExecCount}遍扫描...");
                await Task.Delay(3000);

                lastValidDataCount = aaList.Count(m => !string.IsNullOrWhiteSpace(m.SyncProductPrice) && !string.IsNullOrWhiteSpace(m.SyncProductDescribtion));

                // 模拟一些待处理的任务
                ConcurrentQueue<ExcelFayBuyProduct> modelQueue = new ConcurrentQueue<ExcelFayBuyProduct>();
                foreach (var item in aaList)
                {
                    modelQueue.Enqueue(item);
                }

                //模拟处理机-5台
                var execedList = new ConcurrentBag<string>();
                int theadCount = 5;
                Task[] taskArray = new Task[theadCount];
                for (int i = 0; i < theadCount; i++)
                {
                    taskArray[i] = this.ExecUpdateDataAsync(modelQueue, execedList, aaList.Count);
                }

                await Task.WhenAll(taskArray);

                validDataCount = aaList.Count(m => !string.IsNullOrWhiteSpace(m.SyncProductPrice) && !string.IsNullOrWhiteSpace(m.SyncProductDescribtion));

                // 如果有效店铺数增加，则继续扫描接口数据
                currentExecCount++;
            } while (validDataCount > lastValidDataCount);

            IWorkbook workbook = this.ExcelHelper.CreateOrUpdateWorkbook(aaList);
            this.ExcelHelper.SaveWorkbookToFile(workbook, @$"C:\Users\lixianghong\Desktop\allchinafinds_{DateTime.Now.ToString("HHmmss")}.xlsx");

            await Task.CompletedTask;
        }

        /// <summary>
        /// api/FaybuyProduct/EmportMd
        /// </summary>
        /// <returns></returns>
        [Route("EmportMd")]
        [HttpGet]
        public async Task EmportMd()
        {
            List<ExcelFayBuyProduct> aaList = this.ExcelHelper.ReadTitleDataList<ExcelFayBuyProduct>(@$"C:\Users\lixianghong\Desktop\allchinafinds-合并_133920.xlsx", new ExcelFileDescription());

            int itemIndex = 0;
            int totalCount = aaList.Count;
            foreach (var item in aaList)
            {
                itemIndex++;

                Console.WriteLine($"正在处理第{itemIndex}/{totalCount}条数据...");

                if (string.IsNullOrWhiteSpace(item.SyncProductPrice))
                {
                    continue;
                }

                List<string> categoryNameList = new List<string>(1);
                if (string.IsNullOrWhiteSpace(item.Category_url))
                {
                    categoryNameList.Add("Shoes");
                }
                else
                {
                    string categoryName = item.Category_url.Split("product-category/")[1].Split("/")[0];

                    //分隔-单词，将首字母转大写，改为/拼接
                    List<string> categoryNameArray = categoryName.Split('-').ToList();
                    categoryNameArray = categoryNameArray.Select(x => x[0].ToString().ToUpper() + x.Substring(1).ToLower()).ToList();

                    categoryName = string.Join("/", categoryNameArray);

                    categoryNameList.Add(categoryName);
                }

                decimal cnyRate = (decimal)0.14;
                decimal usdPrice = Math.Round(TypeParseHelper.StrToDecimal(item.SyncProductPrice) * cnyRate, 2);

                string itemMDStr = $@"+++
name = ""{item.Title}""
categories = {JsonConvert.SerializeObject(categoryNameList)}
price = {usdPrice}
image = ""{item.SyncProductImgs}""
kaybuyUrl = ""{item.SyncProductKayBuyUrl}""
+++";

                string productID = item.Url?.Split("id=")[1].Split("&")[0] ?? "";

                //淘宝
                bool isTB = item.Url?.Contains("&source=TB") ?? false;
                //微店
                bool isWD = item.Url?.Contains("&source=WD") ?? false;
                //1688
                bool is1688 = item.Url?.Contains("&source=AL") ?? false;

                string platformName = isTB ? "TB" : (isWD ? "WD" : (is1688 ? "AL" : ""));

                string itemFilePath = @$"C:\Users\lixianghong\Desktop\allchinafinds-合并\{platformName}_{productID}.md";
                if (!System.IO.File.Exists(itemFilePath))
                {
                    await System.IO.File.WriteAllTextAsync(itemFilePath, itemMDStr);
                }
            }
        }

        private async Task ExecUpdateDataAsync(ConcurrentQueue<ExcelFayBuyProduct> modelQueue, ConcurrentBag<string> execedList, int totalCount)
        {
            while (modelQueue.TryDequeue(out ExcelFayBuyProduct item))
            {
                Console.WriteLine($"正在查询第{execedList.Count}/{totalCount}个产品,Url={item.Url}");

                string productID = item.Url?.Split("id=")[1].Split("&")[0] ?? "";

                if (execedList.Contains(productID))
                {
                    execedList.Add(productID);
                    continue;
                }

                execedList.Add(productID);

                bool isPass = false;
                if (item == null || string.IsNullOrWhiteSpace(item.Url))
                {
                    //无效数据，跳过
                    isPass = true;
                }
                else if (!string.IsNullOrWhiteSpace(item.SyncProductPrice)
                    && !string.IsNullOrWhiteSpace(item.SyncProductDescribtion))
                {
                    //如果价格和描述都有值，跳过
                    isPass = true;
                }

                //淘宝
                bool isTB = item.Url?.Contains("&source=TB") ?? false;
                //微店
                bool isWD = item.Url?.Contains("&source=WD") ?? false;
                //1688
                bool is1688 = item.Url?.Contains("&source=AL") ?? false;

                if (isTB)
                {
                    item.SyncProductKayBuyUrl = $"https://kaybuy.com/User/Shop/DetailsNewTB.aspx?urlid={productID}&type=12";
                }
                else if (isWD)
                {
                    item.SyncProductKayBuyUrl = $"https://kaybuy.com/User/Shop/DetailsWeidian.aspx?urlid={productID}&type=14";
                }
                else if (is1688)
                {
                    item.SyncProductKayBuyUrl = $"https://kaybuy.com/User/Shop/Details1688.aspx?urlid={productID}&type=7";
                }

                if (isPass == false)
                {
                    string requestUrl = string.Empty;
                    bool isSkipProduct = false;

                    Func<Task> syncFuncTask = async () =>
                    {
                        if (isTB)
                        {
                            requestUrl = $"https://api-gw.onebound.cn/taobao/item_get?key=t3169987115&secret=7115cf6e&api_name=item_get&result_type=json&lang=en&cache=no&num_iid={productID}";
                        }
                        else if (isWD)
                        {
                            requestUrl = $"https://api-gw.onebound.cn/micro/item_get?key=t3169987115&secret=7115cf6e&api_name=item_get&result_type=json&lang=en&cache=no&num_iid={productID}";
                        }
                        else if (is1688)
                        {
                            requestUrl = $"https://api-gw.onebound.cn/1688/item_get?key=t3169987115&secret=7115cf6e&api_name=item_get&result_type=json&lang=en&cache=no&num_iid={productID}";
                        }

                        (HttpStatusCode, string) getResult = await this.httpClient.Get(requestUrl);
                        JObject pJobj = JObject.Parse(getResult.Item2);

                        if (string.IsNullOrWhiteSpace(item.SyncProductPrice))
                        {
                            item.SyncProductPrice = pJobj.SelectToken("item.price")?.ToString();
                        }
                        if (string.IsNullOrWhiteSpace(item.SyncProductDescribtion))
                        {
                            item.SyncProductDescribtion = pJobj.SelectToken("item.desc")?.ToString();
                        }
                        if (string.IsNullOrWhiteSpace(item.SyncProductImgs))
                        {
                            item.SyncProductImgs = pJobj.SelectToken("item.pic_url")?.ToString();
                        }
                        if (string.IsNullOrWhiteSpace(item.SyncProductTitle))
                        {
                            item.SyncProductTitle = pJobj.SelectToken("item.title")?.ToString();
                        }
                        if (string.IsNullOrWhiteSpace(item.SyncProductOriginData))
                        {
                            item.SyncProductOriginData = getResult.Item2;
                        }

                        if (getResult.Item2.Contains("item-not-found", StringComparison.OrdinalIgnoreCase)
                         || getResult.Item2.Contains("data error", StringComparison.OrdinalIgnoreCase))
                        {
                            isSkipProduct = true;
                        }
                    };

                    do
                    {

                        try
                        {
                            await syncFuncTask();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"查询异常,e={e.Message},Url={item.Url}");
                        }
                    } while (isSkipProduct == false && string.IsNullOrWhiteSpace(item.SyncProductPrice));
                }
            }
        }
    }
}
