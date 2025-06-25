using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using WebApplication1.Helper;
using WebApplication1.Model.ExcelModel;
using WebApplication1.Model.SupabaseModel;

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
        private readonly SupabaseHelper supabaseHelper;

        public FaybuyProductController(
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            IHttpClientFactory httpClientFactory,
            ILogger<OrderShipController> logger,
            SupabaseHelper supabaseHelper)
        {
            this.Logger = logger;
            this.supabaseHelper = supabaseHelper;
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
            string dataSourceFilePath = @$"C:\Users\lixianghong\Desktop\allchinafinds-合并_133920.xlsx";
            List<ExcelFayBuyProduct> aaList = this.ExcelHelper.ReadTitleDataList<ExcelFayBuyProduct>(dataSourceFilePath, new ExcelFileDescription());

            if (true)
            {
                foreach (var item in aaList)
                {
                    decimal cnyRate = (decimal)0.14;
                    decimal usdPrice = Math.Round(TypeParseHelper.StrToDecimal(item.SyncProductPrice) * cnyRate, 2);
                    item.SyncProductPrice = usdPrice.ToString("0.00");

                    List<string> categoryNameList = new List<string>(1);
                    if (string.IsNullOrWhiteSpace(item.Category_url))
                    {
                        categoryNameList.Add("Shoes");
                    }
                    else if (item.Category_url.Contains("//"))
                    {
                        string categoryName = item.Category_url.Split("product-category/")[1].Split("/")[0];

                        //分隔-单词，将首字母转大写，改为/拼接
                        List<string> categoryNameArray = categoryName.Split('-').ToList();
                        categoryNameArray = categoryNameArray.Select(x => x[0].ToString().ToUpper() + x.Substring(1).ToLower()).ToList();

                        categoryName = string.Join("/", categoryNameArray);

                        categoryNameList.Add(categoryName);
                    }
                    else
                    {
                        categoryNameList.Add(item.Category_url);
                    }

                    item.Category_url = JsonConvert.SerializeObject(categoryNameList);
                }
            }

            //int lastValidDataCount, validDataCount = 0;
            //int currentExecCount = 1;
            //do
            //{
            //    Console.WriteLine($"正在执行第{currentExecCount}遍扫描...");
            //    await Task.Delay(3000);

            //    lastValidDataCount = aaList.Count(m => !string.IsNullOrWhiteSpace(m.SyncProductPrice) && !string.IsNullOrWhiteSpace(m.SyncProductDescribtion));

            //    // 模拟一些待处理的任务
            //    ConcurrentQueue<ExcelFayBuyProduct> modelQueue = new ConcurrentQueue<ExcelFayBuyProduct>();
            //    foreach (var item in aaList)
            //    {
            //        modelQueue.Enqueue(item);
            //    }

            //    //模拟处理机-5台
            //    var execedList = new ConcurrentBag<string>();
            //    int theadCount = 5;
            //    Task[] taskArray = new Task[theadCount];
            //    for (int i = 0; i < theadCount; i++)
            //    {
            //        taskArray[i] = this.ExecUpdateDataAsync(modelQueue, execedList, aaList.Count);
            //    }

            //    await Task.WhenAll(taskArray);

            //    validDataCount = aaList.Count(m => !string.IsNullOrWhiteSpace(m.SyncProductPrice) && !string.IsNullOrWhiteSpace(m.SyncProductDescribtion));

            //    // 如果有效店铺数增加，则继续扫描接口数据
            //    currentExecCount++;
            //} while (validDataCount > lastValidDataCount);

            Dictionary<string, ExcelFayBuyProduct> cloneDataDic = new Dictionary<string, ExcelFayBuyProduct>(aaList.Count);
            foreach (var item in aaList)
            {
                if (string.IsNullOrWhiteSpace(item.SyncProductKayBuyUrl))
                {
                    continue;
                }
                if (!cloneDataDic.ContainsKey(item.SyncProductKayBuyUrl))
                {
                    cloneDataDic.Add(item.SyncProductKayBuyUrl, item);
                }
                else if (!string.IsNullOrWhiteSpace(item.SyncProductPrice) && !string.IsNullOrWhiteSpace(item.SyncProductDescribtion))
                {
                    cloneDataDic[item.SyncProductKayBuyUrl] = item;
                }
            }

            Console.WriteLine($"处理结束...");

            IWorkbook workbook = this.ExcelHelper.CreateOrUpdateWorkbook(cloneDataDic.Values.ToList());
            string emportFilePath = @$"C:\Users\lixianghong\Desktop\{DateTime.Now.ToString("HHmmss")}_" + dataSourceFilePath.Split("\\").LastOrDefault();
            this.ExcelHelper.SaveWorkbookToFile(workbook, emportFilePath);

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

                string itemMDStr = $@"+++
name = ""{item.SyncProductTitle}""
categories = {item.Category_url}
price = {item.SyncProductPrice}
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

        /// <summary>
        /// 同步数据到Supabase数据库中
        /// </summary>
        /// <returns></returns>
        public async Task SyncToSupabaseDB()
        {
            var dataList = await this.supabaseHelper.PageAsync<SupabaseProducts>(1, 10);
            //...
        }

        private async Task ExecUpdateDataAsync(ConcurrentQueue<ExcelFayBuyProduct> modelQueue, ConcurrentBag<string> execedList, int totalCount)
        {
            while (modelQueue.TryDequeue(out ExcelFayBuyProduct item))
            {
                Console.WriteLine($"正在查询第{execedList.Count + 1}/{totalCount}个产品,Url={item.Url}");

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
                bool isTB = (item.Url ?? "").Contains("&source=TB") || (item.Url ?? "").Contains("taobao.") || (item.Url ?? "").Contains("tmall.");
                //微店
                bool isWD = (item.Url ?? "").Contains("&source=WD") || (item.Url ?? "").Contains("weidian.");
                //1688
                bool is1688 = (item.Url ?? "").Contains("&source=AL") || (item.Url ?? "").Contains("1688.");

                string productID = null;
                if ((item.Url ?? "").Contains("source="))
                {
                    productID = item.Url?.Split("id=")[1].Split("&")[0] ?? "";
                }

                if (isTB)
                {
                    Uri downloadUrlUri = new Uri(item.Url);
                    var queryString = HttpUtility.ParseQueryString(downloadUrlUri.Query);
                    productID = queryString.Get("id");

                    item.SyncProductKayBuyUrl = $"https://kaybuy.com/User/Shop/DetailsNewTB.aspx?urlid={productID}&type=12";
                }
                else if (isWD)
                {
                    Uri downloadUrlUri = new Uri(item.Url);
                    var queryString = HttpUtility.ParseQueryString(downloadUrlUri.Query);
                    productID = queryString.Get("itemID");

                    item.SyncProductKayBuyUrl = $"https://kaybuy.com/User/Shop/DetailsWeidian.aspx?urlid={productID}&type=14";
                }
                else if (is1688)
                {
                    string downloadUrl = item.Url.Split('?')[0];
                    productID = downloadUrl.Split('/').LastOrDefault().Split('.')[0];
                    item.SyncProductKayBuyUrl = $"https://kaybuy.com/User/Shop/Details1688.aspx?urlid={productID}&type=7";
                }

                //已处理过的产品不再继续
                if (string.IsNullOrWhiteSpace(productID) || execedList.Contains(productID))
                {
                    execedList.Add(productID);
                    continue;
                }
                execedList.Add(productID);

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

                            decimal cnyRate = (decimal)0.14;
                            decimal usdPrice = Math.Round(TypeParseHelper.StrToDecimal(item.SyncProductPrice) * cnyRate, 2);
                            item.SyncProductPrice = usdPrice.ToString("0.00");
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

                        //处理品类
                        List<string> categoryNameList = new List<string>(1);
                        if (string.IsNullOrWhiteSpace(item.Category_url))
                        {
                            categoryNameList.Add("Shoes");
                        }
                        else if (item.Category_url.Contains("//"))
                        {
                            string categoryName = item.Category_url.Split("product-category/")[1].Split("/")[0];

                            //分隔-单词，将首字母转大写，改为/拼接
                            List<string> categoryNameArray = categoryName.Split('-').ToList();
                            categoryNameArray = categoryNameArray.Select(x => x[0].ToString().ToUpper() + x.Substring(1).ToLower()).ToList();

                            categoryName = string.Join("/", categoryNameArray);

                            categoryNameList.Add(categoryName);
                        }
                        else
                        {
                            categoryNameList.Add(item.Category_url);
                        }
                        item.Category_url = JsonConvert.SerializeObject(categoryNameList);

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
