using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.BIZ;
using WebApplication1.Helper;
using WebApplication1.Model.ExcelModel;
using WebApplication1.Model.MeShop;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 订单发货数据导出控制器
    /// </summary>
    [Route("api/ShouYunEmail")]
    [ApiController]
    public class ShouYunEmailController : ControllerBase
    {
        protected HttpClient PayHttpClient;
        public ExcelHelper ExcelHelper;
        public IWebHostEnvironment WebHostEnvironment;
        public ILogger Logger;

        public ShouYunEmailController(
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            ILogger<OrderShipController> logger)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
            this.Logger = logger;
        }


        /// <summary>
        /// 发送Excel订单发货记录到MeShop站点
        /// api/ShouYunEmail/SaveEmail
        /// </summary>
        /// <returns></returns>
        [Route("SaveEmail")]
        [HttpGet]
        public async Task SaveEmail()
        {
            string contentRootPath = this.WebHostEnvironment.ContentRootPath;
            string emailFilePath = $@"{contentRootPath}\示例测试目录\首云邮箱\邮箱.xlsx";
            List<ExcelShouYunEmail> dataList = this.ExcelHelper.ReadTitleDataList<ExcelShouYunEmail>(emailFilePath, new ExcelFileDescription(0));
            string postUrl = $@"http://mailadm.mailserverbd.com/Users/edit";
            int index = 0;
            int totalCount = dataList.Count;
            List<string> errorEmailList = new List<string>(totalCount);
            foreach (ExcelShouYunEmail emailObj in dataList)
            {
                index++;
                Dictionary<string, string> formDic = new Dictionary<string, string>()
                {
                    {"email",emailObj.Email },
                    {"uname",""  },
                    {"tel",""  },
                    {"active","1"  },
                    {"password",emailObj.Password  },
                    {"password2",emailObj.Password  },
                    {"_method","put"  },
                    {"_forward","%2FUsers"  }
                };
                Dictionary<string, string> headDic = new Dictionary<string, string>()
                {
                    { "X-Requested-With","XMLHttpRequest"},
                    {"Cookie","PHPSESSID=dpop4f5stjt544nlbnclue0hr1" }
                };
                var postResult = await this.PayHttpClient.PostForm(postUrl, formDic, headDic);
                string result = $"失败：{postResult.Item1}_{postResult.Item2}";
                if (postResult.Item1 == System.Net.HttpStatusCode.OK)
                {
                    int status = 0;
                    try
                    {
                        status = JObject.Parse(postResult.Item2).SelectToken("status").ToObject<int>();
                    }
                    catch
                    {

                    }

                    if (status == 1)
                    {
                        result = "成功";
                    }
                    else
                    {
                        errorEmailList.Add(emailObj.Email);
                    }
                }
                this.Logger.LogInformation($"正在添加第{index}/{totalCount}个邮箱{emailObj.Email}结果：{result}");
            }

            this.Logger.LogInformation($"同步结束. 共{totalCount}个邮箱,成功{totalCount - errorEmailList.Count}个,失败{errorEmailList.Count}个：{string.Join(',', errorEmailList)}");
        }
    }
}
