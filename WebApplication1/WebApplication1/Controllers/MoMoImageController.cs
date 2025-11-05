using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Web;
using WebApplication1.DB.CMS;
using WebApplication1.DB.Repository;
using WebApplication1.Helper;
using WebApplication1.Model.ExcelModel;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// MoMoImage业务控制器
    /// </summary>
    [Route("api/MoMoImage")]
    [ApiController]
    public class MoMoImageController : ControllerBase
    {
        protected HttpClient PayHttpClient;
        public ExcelHelper ExcelHelper;
        private readonly IWebHostEnvironment WebHostEnvironment;
        public ILogger Logger;
        private readonly Lazy<Wd_ThirdProductListRepository> wd_ThirdProductListRepository;
        private Dictionary<string, byte[]> imgUrlDic = new Dictionary<string, byte[]>(100);

        public MoMoImageController(
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            ILogger<OrderShipController> logger,
            Lazy<Wd_ThirdProductListRepository> wd_ThirdProductListRepository)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.Logger = logger;
            this.wd_ThirdProductListRepository = wd_ThirdProductListRepository;
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// 微店店铺产品数据同步
        /// api/MoMoImage/WeiDianShopProductSync?shopID=1677568739&collName=jersey
        /// </summary>
        /// <returns></returns>
        [Route("WeiDianShopProductSync")]
        [HttpGet]
        public async Task WeiDianShopProductSync(string shopID, string collName)
        {
            string paramTemplate = "{\"shopId\":\"{shopID}\",\"tabId\":0,\"sortOrder\":\"desc\",\"offset\":{offset},\"limit\":20,\"from\":\"h5\",\"showItemTag\":true}";

            int page = 1;
            int pageSize = 20;

            int currentPageSize = 0;

            List<Wd_ThirdProductList> fileDataList = new List<Wd_ThirdProductList>();
            do
            {
                int offset = (page - 1) * pageSize;
                string param = paramTemplate.Replace("{shopID}", shopID).Replace("{offset}", offset.ToString());

                string syncUrl = $"https://thor.weidian.com/decorate/shopDetail.tab.getItemList/1.0?param={HttpUtility.UrlEncode(param)}";

                var getResult = await this.PayHttpClient.Get(syncUrl, new Dictionary<string, string>
                {
                    { "Referer", "https://weidian.com/" }
                });
                JObject jObj = JObject.Parse(getResult.Item2);
                JArray pageList = jObj.SelectToken("result.itemList").ToObject<JArray>();
                currentPageSize = pageList.Count;

                if (currentPageSize > 0)
                {
                    foreach (JObject item in pageList)
                    {
                        fileDataList.Add(new Wd_ThirdProductList
                        {
                            Wt_AddTime = DateTime.Now,
                            Wt_CurrentGuID = Guid.NewGuid().ToString(),
                            Wt_IsDelete = 0,
                            Wt_IsTrue = 1,
                            Wt_OriginCollName = collName,
                            Wt_OriginProductID = item.SelectToken("itemId").ToString(),
                            Wt_OriginProductMall = "weidian",
                            Wt_Title = item.SelectToken("itemName").ToString(),
                            //移除额外参数
                            Wt_Image = item.SelectToken("itemImg").ToString().Split('?')[0],
                            Wt_Price = item.SelectToken("price").ToObject<decimal>()
                        });
                    }
                }
                page++;
            } while (currentPageSize == pageSize);

            await this.wd_ThirdProductListRepository.Value.DeleteCollAsync(collName);
            await this.wd_ThirdProductListRepository.Value.AddListAsync(fileDataList.ToArray());

            Console.WriteLine("1");
        }

        /// <summary>
        /// 下载产品图
        /// api/MoMoImage/ExecImage
        /// </summary>
        /// <returns></returns>
        [Route("ExecImage")]
        [HttpGet]
        public async Task ExecImage()
        {
            //1-设置数据源
            string dataDicPath = @$"E:\公司小项目\产品图片收集\数据源\9月上架品类-5-橄榄球-数据整理.xlsx";
            //2-设置保存目录
            string savePath = @"E:\公司小项目\产品图片收集\数据源\9月上架品类-5-橄榄球";

            List<ExcelImageData_MoMo> fileDataList = this.ExcelHelper.ReadTitleDataList<ExcelImageData_MoMo>(dataDicPath, new ExcelFileDescription());

            foreach (ExcelImageData_MoMo item in fileDataList)
            {
                //if (TypeParseHelper.StrToInt32(item.Index) < 75)
                //{
                //    continue;
                //}
                Console.WriteLine($"正在处理第{item.Index}行图片数据...{item.Remark}");

                string fileParentPath = @$"{savePath}\{item.Index}";

                if (Directory.Exists(fileParentPath) && Directory.GetFiles(fileParentPath).Length > 0)
                {
                    Console.WriteLine($"文件已下载,跳过...{fileParentPath}");
                    continue;
                }

                string itemHost = "https://" + item.ProductUrl.Split("https://")[1].Split("/")[0];
                string[] imgUrls = item.Remark?.Split('|').ToArray() ?? new string[0];
                string filePath = "";
                string fileExtendName = "";
                int filePosition = 1;

                imgUrls = imgUrls.Distinct().ToArray();

                if (imgUrls.Length > 0)
                {
                    if (!Directory.Exists(fileParentPath))
                    {
                        Directory.CreateDirectory(fileParentPath);
                    }
                }

                foreach (string imgUrl in imgUrls)
                {
                    Console.WriteLine($"正在处理第{filePosition}/{imgUrls.Length}个图片...");

                    string currentImgUrl = imgUrl;
                    if (!imgUrl.StartsWith("https"))
                    {
                        currentImgUrl = $"{itemHost}/{imgUrl.TrimStart('/')}";
                    }
                    //移除url参数
                    currentImgUrl = currentImgUrl.Split("?")[0];

                    fileExtendName = currentImgUrl.Split('/').Last().Split('.').Last();
                    if (!"jpg,jpeg,png".Contains(fileExtendName))
                    {
                        fileExtendName = "jpg";
                    }

                    filePath = @$"{fileParentPath}\{filePosition}.{fileExtendName}";

                    await this.Download(currentImgUrl, filePath);

                    filePosition++;
                }
            }

            Console.WriteLine($"任务结束...");
        }

        /// <summary>
        /// 订单发货数据过滤
        /// api/MoMoImage/CollExcelProduct
        /// </summary>
        /// <returns></returns>
        [Route("CollExcelProduct")]
        [HttpGet]
        public async Task CollExcelProduct()
        {
            await Task.CompletedTask;
            //1-设置数据源
            string dataDicPath = @$"C:\Users\lixianghong\Desktop\snack-零食-20251105.xlsx";
            string dataCollName = "snack";

            List<ExcelProductData_MoMo> fileDataList = this.ExcelHelper.ReadTitleDataList<ExcelProductData_MoMo>(dataDicPath, new ExcelFileDescription());

            List<string> sqlList = new List<string>();

            string detailTaobaoApiUrl = "https://api-gw.onebound.cn/taobao/item_get?key=t3169987115&secret=7115cf6e&api_name=item_get&result_type=json&num_iid={productID}";

            int index = 0;
            foreach (ExcelProductData_MoMo item in fileDataList)
            {
                index++;
                if (index <= 14)
                {
                    continue;
                }

                Console.WriteLine($"正在处理第{index}/{fileDataList.Count}条数据...");

                //获取产品地址分类
                var productIdResult = this.GetProductIdByUrl(item.Wt_ProductUrl);
                if (productIdResult.Item1 == null || string.IsNullOrWhiteSpace(productIdResult.Item2))
                {
                    continue;
                }
                string productID = productIdResult.Item2;

                //获取产品原始数据
                string productTitle = null;
                string productOriginData = null;
                if (productIdResult.Item1 == EMallPlatform.淘宝)
                {
                    string requestUrl = detailTaobaoApiUrl.Replace("{productID}", productID);
                    var getResult = await this.PayHttpClient.Get(requestUrl);
                    productOriginData = getResult.Item2;
                    JObject proJObj = JObject.Parse(productOriginData);
                    productTitle = proJObj.SelectToken("item.title")?.ToString();
                }

                if (string.IsNullOrWhiteSpace(productTitle))
                {
                    continue;
                }

                //生成插入脚本
                string productPlatformName = "taobao";
                if (productIdResult.Item1 == EMallPlatform.官方1688)
                {
                    productPlatformName = "1688";
                }
                else if (productIdResult.Item1 == EMallPlatform.微店)
                {
                    productPlatformName = "weidian";
                }

                string insertProductSql = $@"
                        INSERT INTO dbo.Wd_ThirdProductList
                                ( Wt_Price ,
                                  Wt_OriginProductMall ,
                                  Wt_OriginProductID ,
                                  Wt_IsDelete ,
                                  Wt_CurrentGuID ,
                                  Wt_IsTrue ,
                                  Wt_OrderID ,
                                  Wt_AddTime ,
                                  Wt_UpdateTime ,
                                  Wt_OriginProductDataJson ,
                                  Wt_OriginProductUnionKey ,
                                  Wt_IsAutoSync
                                )
                        VALUES  ( {item.Wt_ProductPrice} , -- Wt_Price - decimal
                                  '{productPlatformName}' , -- Wt_OriginProductMall - varchar(10)
                                  '{productID}' , -- Wt_OriginProductID - varchar(100)
                                  0 , -- Wt_IsDelete - bit
                                  N'{Guid.NewGuid().ToString()}' , -- Wt_CurrentGuID - nvarchar(50)
                                  1 , -- Wt_IsTrue - bit
                                  0 , -- Wt_OrderID - int
                                  GETDATE() , -- Wt_AddTime - datetime
                                  GETDATE() , -- Wt_UpdateTime - datetime
                                  N'{productOriginData}' , -- Wt_OriginProductDataJson - nvarchar(max)
                                  N'{productPlatformName}_{productID}' , -- Wt_OriginProductUnionKey - nvarchar(110)
                                  0  -- Wt_IsAutoSync - bit
                                )
                        ";

                string insertProductCollSql = $@"
                        INSERT INTO dbo.Wd_ThirdProductColl
                                ( Wt_Title ,
                                    Wt_OriginProductMall ,
                                    Wt_OriginProductID ,
                                    Wt_OriginProductUnionKey ,
                                    Wt_IsDelete ,
                                    Wt_CurrentGuID ,
                                    Wt_IsTrue ,
                                    Wt_OrderID ,
                                    Wt_AddTime ,
                                    Wt_UpdateTime
                                )
                        VALUES  ( N'{dataCollName}' , -- Wt_Title - nvarchar(200)
                                    N'{productPlatformName}' , -- Wt_OriginProductMall - nvarchar(10)
                                    N'{productID}' , -- Wt_OriginProductID - nvarchar(100)
                                    N'{productPlatformName}_{productID}' , -- Wt_OriginProductUnionKey - nvarchar(110)
                                    0 , -- Wt_IsDelete - bit
                                    N'{Guid.NewGuid().ToString()}' , -- Wt_CurrentGuID - nvarchar(50)
                                    1 , -- Wt_IsTrue - bit
                                    0 , -- Wt_OrderID - int
                                    GETDATE() , -- Wt_AddTime - datetime
                                    GETDATE()  -- Wt_UpdateTime - datetime
                                )
                ";

                sqlList.Add(insertProductSql);
                sqlList.Add(insertProductCollSql);
            }

            string sql = string.Join(";", sqlList) + ";";
            Console.WriteLine(sql);
        }

        private async Task Download(string webFileUrl, string filePath)
        {
            //如果图片已下载，直接保存
            if (this.imgUrlDic.ContainsKey(webFileUrl))
            {
                var imageBytes = this.imgUrlDic[webFileUrl];
                System.IO.File.WriteAllBytes(filePath, imageBytes);
                return;
            }

            var client = new HttpClient();
            //client.DefaultRequestVersion = HttpVersion.Version11;
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,zh;q=0.9,en;q=0.8");

            if (webFileUrl.Contains("www.vancleefarpels.com"))
            {
                //设置访问头
                client.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Not;A=Brand\";v=\"99\", \"Google Chrome\";v=\"139\", \"Chromium\";v=\"139\"");
                client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
                client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
                client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            }
            else if (webFileUrl.Contains("m.media-amazon.com"))
            {
                //移除尺寸标记
                webFileUrl = webFileUrl.Replace("38,50_", "");
            }
            else if (webFileUrl.Contains("stadiumgoods.com"))
            {
                //移除尺寸标记
                webFileUrl = webFileUrl.Replace("38,50_", "");
            }
            else if (webFileUrl.Contains("fansidea.com"))
            {
                //移除尺寸标记
                webFileUrl = webFileUrl.Replace("_800x", "");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, webFileUrl);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode)
            {
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                this.imgUrlDic.Add(webFileUrl, imageBytes);
                System.IO.File.WriteAllBytes(filePath, imageBytes);
            }
        }

        /// <summary>
        /// 根据产品原始链接获取产品ID
        /// </summary>
        /// <param name="productUrl"></param>
        /// <returns></returns>
        private (EMallPlatform?, string) GetProductIdByUrl(string productUrl)
        {
            if (string.IsNullOrWhiteSpace(productUrl))
            {
                return (null, "");
            }
            if (!productUrl.StartsWith("https:"))
            {
                return (null, productUrl);
            }

            string? productID = productUrl;
            EMallPlatform mallPlatform = EMallPlatform.其他;

            Uri downloadUrlUri = new Uri(productID);
            var queryString = HttpUtility.ParseQueryString(downloadUrlUri.Query);

            //1688产品
            string itemDownloadUrlNoParam = productID.Split('?')[0];
            if (itemDownloadUrlNoParam.Contains("1688."))
            {
                string downloadUrl = productID.Split('?')[0];
                productID = downloadUrl.Split('/').LastOrDefault()?.Split('.')[0];
                mallPlatform = EMallPlatform.官方1688;
            }
            //淘宝+天猫产品
            else if (itemDownloadUrlNoParam.Contains("taobao.") || itemDownloadUrlNoParam.Contains("tmall."))
            {
                productID = queryString.Get("id");
                mallPlatform = EMallPlatform.淘宝;
            }
            //微店产品
            else if (itemDownloadUrlNoParam.Contains("weidian."))
            {
                productID = queryString.Get("itemID");
                mallPlatform = EMallPlatform.微店;
            }

            return (mallPlatform, productID ?? productUrl);
        }

        /// <summary>
        /// 店铺平台枚举
        /// </summary>
        private enum EMallPlatform
        {
            /// <summary>
            /// 其他
            /// </summary>
            其他 = 0,

            /// <summary>
            /// 1688
            /// </summary>
            官方1688 = 7,
            /// <summary>
            /// 淘宝
            /// </summary>
            淘宝 = 12,
            /// <summary>
            /// 微店
            /// </summary>
            微店 = 14,
        }
    }
}
