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
        /// 订单发货数据过滤
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
            string dataDicPath = @$"C:\Users\lixianghong\Desktop\三方产品列表整理.xlsx";
            //2-设置保存目录
            string savePath = $@"C:\Users\lixianghong\Desktop\三方产品列表整理_{DateTime.Now.ToString("HHmmss")}.xlsx";

            List<ExcelProductData_MoMo> fileDataList = this.ExcelHelper.ReadTitleDataList<ExcelProductData_MoMo>(dataDicPath, new ExcelFileDescription());

            List<string> sqlList = new List<string>();

            foreach (ExcelProductData_MoMo item in fileDataList)
            {
                sqlList.Add($"INSERT INTO dbo.Wd_ThirdProductList ( Wt_Title , Wt_Image , Wt_Price , Wt_OriginProductMall , Wt_OriginProductID , Wt_IsDelete , Wt_CurrentGuID , Wt_IsTrue , Wt_OrderID , Wt_AddTime) VALUES ( '{item.Wt_Title}' , '{item.Wt_Image}' , {item.Wt_Price} , '{item.Wt_OriginProductMall}' , '{item.Wt_OriginProductID}' , 0 , N'{Guid.NewGuid().ToString()}' , 1 , 0 , GETDATE())");
            }

            string sql = string.Join(";", sqlList);
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
    }
}
