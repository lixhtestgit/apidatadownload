using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
            List<ExcelFayBuyProduct> aaList = this.ExcelHelper.ReadTitleDataList<ExcelFayBuyProduct>(@$"C:\Users\lixianghong\Desktop\allchinafinds-2_194127.xlsx", new ExcelFileDescription());

            int totalCount = aaList.Count;
            int currentIndex = 0;

            foreach (ExcelFayBuyProduct item in aaList)
            {
                if (string.IsNullOrWhiteSpace(item.Url))
                {
                    currentIndex++;
                    continue;
                }
                //淘宝
                bool isTB = item.Url.Contains("&source=TB");
                //微店
                bool isWD = item.Url.Contains("&source=WD");
                //微店
                bool is1688 = item.Url.Contains("&source=AL");

                string productID = item.Url.Split("id=")[1].Split("&")[0];

                string requestUrl = string.Empty;
                bool isSkipProduct = false;

                Func<Task> syncActionTask = async () =>
                {
                    if (isTB)
                    {
                        item.SyncProductFayBuyUrl = $"https://kaybuy.com/User/Shop/DetailsNewTB.aspx?urlid={productID}&type=12";
                        requestUrl = $"https://api-gw.onebound.cn/taobao/item_get?key=t3169987115&secret=7115cf6e&api_name=item_get&result_type=json&lang=cn&num_iid={productID}";
                    }
                    else if (isWD)
                    {
                        item.SyncProductFayBuyUrl = $"https://kaybuy.com/User/Shop/DetailsWeidian.aspx?urlid={productID}&type=12";
                        requestUrl = $"https://api-gw.onebound.cn/micro/item_get?key=t3169987115&secret=7115cf6e&api_name=item_get&result_type=json&lang=en&num_iid={productID}";
                    }
                    else if (is1688)
                    {
                        item.SyncProductFayBuyUrl = $"https://kaybuy.com/User/Shop/Details1688.aspx?urlid={productID}&type=7";
                        requestUrl = $"https://api-gw.onebound.cn/1688/item_get?key=t3169987115&secret=7115cf6e&api_name=item_get&result_type=json&lang=cn&num_iid={productID}";
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

                    if (isWD && getResult.Item2.Contains("item-not-found", StringComparison.OrdinalIgnoreCase))
                    {
                        isSkipProduct = true;
                    }
                    else if (is1688 && getResult.Item2.Contains("data error", StringComparison.OrdinalIgnoreCase))
                    {
                        isSkipProduct = true;
                    }
                };

                do
                {
                    Console.WriteLine($"正在查询第{currentIndex + 1}/{totalCount}个产品,Url={item.Url}");

                    //如果价格和描述都有值，则跳过
                    if (!string.IsNullOrWhiteSpace(item.SyncProductPrice)
                        && !string.IsNullOrWhiteSpace(item.SyncProductDescribtion))
                    {
                        isSkipProduct = true;
                    }
                    else
                    {
                        try
                        {
                            await syncActionTask();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"查询异常,e={e.Message},Url={item.Url}");
                        }
                    }
                } while (isSkipProduct == false && string.IsNullOrWhiteSpace(item.SyncProductPrice));

                currentIndex++;
            }

            IWorkbook workbook = this.ExcelHelper.CreateOrUpdateWorkbook(aaList);
            this.ExcelHelper.SaveWorkbookToFile(workbook, @$"C:\Users\lixianghong\Desktop\allchinafinds-2_{DateTime.Now.ToString("HHmmss")}.xlsx");

            await Task.CompletedTask;
        }
    }
}
