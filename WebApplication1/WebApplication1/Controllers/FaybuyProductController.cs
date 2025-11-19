using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using WebApplication1.DB.Repository;
using WebApplication1.ExcelCsv;
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
        public CsvHelper csvHelper;
        private readonly HttpClient httpClient;
        private readonly IWebHostEnvironment WebHostEnvironment;
        public ILogger Logger;
        private readonly SupabaseHelper supabaseHelper;
        private readonly GoogleTranslateHelper googleTranslateHelper;
        private readonly Wd_PromotionLinkRepository wd_PromotionLinkRepository;

        public FaybuyProductController(
            CsvHelper csvHelper,
            IWebHostEnvironment webHostEnvironment,
            IHttpClientFactory httpClientFactory,
            ILogger<OrderShipController> logger,
            SupabaseHelper supabaseHelper,
            GoogleTranslateHelper googleTranslateHelper,
            Wd_PromotionLinkRepository wd_PromotionLinkRepository)
        {
            this.Logger = logger;
            this.supabaseHelper = supabaseHelper;
            this.googleTranslateHelper = googleTranslateHelper;
            this.wd_PromotionLinkRepository = wd_PromotionLinkRepository;
            this.csvHelper = csvHelper;
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
            string dataSourceFilePath = @$"C:\Users\lixianghong\Desktop\待处理.csv";
            List<ExcelFayBuyProduct> aaList = this.csvHelper.Read<ExcelFayBuyProduct>(dataSourceFilePath, new CsvFileDescription());

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
                    taskArray[i] = this.ExecUpdateExcelDataAsync(modelQueue, execedList, aaList.Count);
                }

                await Task.WhenAll(taskArray);

                validDataCount = aaList.Count(m => !string.IsNullOrWhiteSpace(m.SyncProductPrice) && !string.IsNullOrWhiteSpace(m.SyncProductDescribtion));

                // 如果有效店铺数增加，则继续扫描接口数据
                currentExecCount++;
            } while (validDataCount > lastValidDataCount);

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

            string emportFilePath = @$"C:\Users\lixianghong\Desktop\{DateTime.Now.ToString("HHmmss")}_" + dataSourceFilePath.Split("\\").LastOrDefault();
            this.csvHelper.WriteFile(emportFilePath, cloneDataDic.Values.ToList(), new CsvFileDescription());

            Console.WriteLine($"处理结束...");

            await Task.CompletedTask;
        }

        /// <summary>
        /// api/FaybuyProduct/UpdateUrlForSupabaseDB
        /// 更新Supabase数据库中Url域名
        /// </summary>
        /// <returns></returns>
        [Route("UpdateUrlForSupabaseDB")]
        [HttpGet]
        public async Task UpdateUrlForSupabaseDB()
        {
            bool isEnd = false;
            int pageIndex = 1;
            int pageSize = 100;

            int execCount = 0;

            do
            {
                try
                {
                    List<SupabaseProducts> pageDataList = await this.supabaseHelper.PageAsync<SupabaseProducts>(pageIndex, pageSize);

                    foreach (SupabaseProducts item in pageDataList)
                    {
                        execCount++;
                        this.Logger.LogInformation($"正在同步第{execCount}个产品Url...");

                        Uri itemUrlUri = new Uri(item.Link);
                        var queryString = HttpUtility.ParseQueryString(itemUrlUri.Query);

                        string productID = queryString.Get("urlid");
                        if (string.IsNullOrWhiteSpace(productID))
                        {
                            continue;
                        }

                        queryString.Remove("urlid");
                        queryString.Remove("type");
                        string newQueryStr = queryString.ToString();

                        if (item.Link.Contains("DetailsNewTB", StringComparison.OrdinalIgnoreCase))
                        {
                            item.Link = $"https://kaybuy.com/product/taobao/{productID}";
                        }
                        else if (item.Link.Contains("weidian", StringComparison.OrdinalIgnoreCase))
                        {
                            item.Link = $"https://kaybuy.com/product/weidian/{productID}";
                        }
                        else if (item.Link.Contains("1688", StringComparison.OrdinalIgnoreCase))
                        {
                            item.Link = $"https://kaybuy.com/product/1688/{productID}";
                        }

                        if (!string.IsNullOrWhiteSpace(newQueryStr))
                        {
                            item.Link += $"?{newQueryStr}";
                        }

                        await this.supabaseHelper.UpdateAsync(item);
                    }

                    if (pageDataList.Count == 0)
                    {
                        isEnd = true;
                    }

                    pageIndex++;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"同步失败：{e.Message}");
                }
            } while (isEnd == false);

            Console.WriteLine("处理结束...");
        }

        private async Task ExecUpdateExcelDataAsync(ConcurrentQueue<ExcelFayBuyProduct> modelQueue, ConcurrentBag<string> execedList, int totalCount)
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

                //如果Url不是链接，默认为微店
                if (!(item.Url ?? "").StartsWith("https://"))
                {
                    isWD = true;
                }

                //收集产品ID
                string productID = item.Url ?? "";
                if (string.IsNullOrWhiteSpace(productID))
                {
                    return;
                }
                if (productID.StartsWith("https://"))
                {
                    if (productID.Contains("source="))
                    {
                        productID = item.Url?.Split("id=")[1].Split("&")[0] ?? "";
                    }

                    if (isTB)
                    {
                        Uri downloadUrlUri = new Uri(item.Url);
                        var queryString = HttpUtility.ParseQueryString(downloadUrlUri.Query);
                        productID = queryString.Get("id");
                    }
                    else if (isWD)
                    {
                        Uri downloadUrlUri = new Uri(item.Url);
                        var queryString = HttpUtility.ParseQueryString(downloadUrlUri.Query);
                        productID = queryString.Get("itemID");
                    }
                    else if (is1688)
                    {
                        string downloadUrl = item.Url.Split('?')[0];
                        productID = downloadUrl.Split('/').LastOrDefault().Split('.')[0];
                    }
                }

                //初始化产品系统链接
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
                            item.SyncProductOriginPrice = pJobj.SelectToken("item.orginal_price")?.ToString();

                            decimal cnyRate = (decimal)0.14;
                            decimal usdPrice = Math.Round(TypeParseHelper.StrToDecimal(item.SyncProductPrice) * cnyRate, 2);
                            decimal usdOriginPrice = Math.Round(TypeParseHelper.StrToDecimal(item.SyncProductOriginPrice) * cnyRate, 2);
                            item.SyncProductPrice = usdPrice.ToString("0.00");
                            item.SyncProductOriginPrice = usdOriginPrice.ToString("0.00");
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

                        //处理品类(要求首字母大写)
                        if (string.IsNullOrWhiteSpace(item.Category_url))
                        {
                            item.Category_url = @"[""Bags""]";
                        }
                        else if (item.Category_url.StartsWith("https://"))
                        {
                            List<string> categoryNameList = new List<string>(1);

                            string categoryName = item.Category_url.Split("product-category/")[1].Split("/")[0];

                            //分隔-单词，将首字母转大写，改为/拼接
                            List<string> categoryNameArray = categoryName.Split('-').ToList();
                            categoryNameArray = categoryNameArray.Select(x => x[0].ToString().ToUpper() + x.Substring(1).ToLower()).ToList();
                            categoryName = string.Join("/", categoryNameArray);
                            categoryNameList.Add(categoryName);

                            item.Category_url = JsonConvert.SerializeObject(categoryNameList);
                        }
                        else if (!item.Category_url.StartsWith("["))
                        {
                            List<string> categoryNameArray = item.Category_url.Split('-').ToList();
                            categoryNameArray = categoryNameArray.Select(x => x[0].ToString().ToUpper() + x.Substring(1).ToLower()).ToList();

                            item.Category_url = $@"[""{string.Join("/", categoryNameArray)}""]";
                        }
                        else
                        {
                            //...已处理
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
