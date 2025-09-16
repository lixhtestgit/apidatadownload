using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private Dictionary<string, byte[]> imgUrlDic = new Dictionary<string, byte[]>(100);

        public MoMoImageController(
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
        /// api/MoMoImage/ExecImage
        /// </summary>
        /// <returns></returns>
        [Route("ExecImage")]
        [HttpGet]
        public async Task ExecImage()
        {
            //1-设置数据源
            string dataDicPath = @$"E:\公司小项目\产品图片收集\数据源\9月上架品类-4-球鞋-数据整理.xlsx";
            //2-设置保存目录
            string savePath = @"E:\公司小项目\产品图片收集\数据源\9月上架品类-4-球鞋";

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
