﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebApplication1.Enum;
using WebApplication1.Helper;
using WebApplication1.Model;

namespace WebApplication1.Controllers
{
    [Route("api/PayNotify")]
    [ApiController]
    public class PayNotifyController : ControllerBase
    {
        protected HttpClient PayHttpClient { get; set; }
        public ExcelHelper ExcelHelper { get; set; }
        public IWebHostEnvironment WebHostEnvironment { get; set; }
        public ILogger Logger { get; set; }
        public IConfiguration Configuration { get; set; }
        public ESSearchHelper ESSearchHelper { get; set; }

        public PayNotifyController(
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            ILogger<TestController> logger,
            IConfiguration configuration,
            ESSearchHelper eSSearchHelper)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
            this.Logger = logger;
            this.Configuration = configuration;
            this.ESSearchHelper = eSSearchHelper;
        }

        /// <summary>
        /// ES搜索订单支付方式
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task<IActionResult> ESSearchOrderPayType()
        {
            string dataFilter = @"[
    {
        ""multi_match"": {
            ""type"": ""phrase"",
            ""query"": ""MeShopPay向网站发送支付结果失败"",
            ""lenient"": true
        }
    }
]";

            List<ESLog> esLogList = await this.ESSearchHelper.GetESLogList($"获取Pay通知失败数据", dataFilter, 6, log =>
            {
                return "1";
            });

            int totalCount = esLogList.Count;
            this.Logger.LogInformation($"获取到同步失败数据共{totalCount}个");
            string sessionID, notifyUrl;
            Regex sessionIDRegex = new Regex("(?<=\"token\":\")[a-z0-9]+(?=\")");
            Regex notifyUrlRegex = new Regex("(?<=\"NotifyUrl\":\")[^\"]+(?=\")");
            int position = 1;
            bool isSend = false;
            foreach (ESLog log in esLogList)
            {
                sessionID = sessionIDRegex.Match(log.Log).Value;
                notifyUrl = notifyUrlRegex.Match(log.Log).Value + "?sessionID=" + sessionID;
                isSend = false;
                do
                {
                    var postResult = await this.PayHttpClient.Post(notifyUrl, null);
                    isSend = postResult.Item1 == System.Net.HttpStatusCode.OK;
                    this.Logger.LogInformation($"正在同步{position}/{totalCount}个异步通知消息到网站:{postResult.Item1},detail:{{notifyUrl={notifyUrl}}}");
                } while (isSend == false);

                position++;
            }

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }
    }
}