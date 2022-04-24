using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
using WebApplication1.Helper;
using WebApplication1.Model;

namespace WebApplication1.Controllers
{
    [Route("api/Order")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        protected HttpClient PayHttpClient { get; set; }
        public ExcelHelper ExcelHelper { get; set; }
        public IWebHostEnvironment WebHostEnvironment { get; set; }
        public ILogger Logger { get; set; }
        public IConfiguration Configuration { get; set; }
        public ESSearchHelper ESSearchHelper { get; set; }
        public IMemoryCache MemoryCache { get; set; }

        public OrderController(
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            ILogger<TestController> logger,
            IConfiguration configuration,
            ESSearchHelper eSSearchHelper,
            IMemoryCache memoryCache)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
            this.Logger = logger;
            this.Configuration = configuration;
            this.ESSearchHelper = eSSearchHelper;
            this.MemoryCache = memoryCache;
        }

        /// <summary>
        /// ES搜索订单支付方式
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task<IActionResult> ESSearchOrderPayType()
        {
            string templateName = "Template1";

            string filePath = @"C:\Users\lixianghong\Desktop\Test.xlsx";
            List<Order> dataList = new List<Order>(1000);

            IWorkbook workbook = null;
            try
            {
                IFileProvider fileProvider = this.WebHostEnvironment.ContentRootFileProvider;
                IFileInfo fileInfo = fileProvider.GetFileInfo($"en-{templateName}.json");

                string fileContent = null;
                using (StreamReader readSteam = new StreamReader(fileInfo.CreateReadStream()))
                {
                    fileContent = await readSteam.ReadToEndAsync();
                }
                JObject templateFileJObj = JObject.Parse(fileContent);
                JArray pageJPropertyList = templateFileJObj.SelectToken("data.Results").ToObject<JArray>();

                if (pageJPropertyList.Count() == 0)
                {
                    throw new Exception($"未找到MyData_{templateName}的配置数据");
                }

                dataList.AddRange(ExcelHelper.ReadTitleDataList<Order>(filePath, new ExcelFileDescription()));
                this.Logger.LogInformation($"已导出数据共{dataList.Count}个.");
                //前368个重新查询，查询时间错误
                int position = dataList.Count + 1;
                int totalCount = pageJPropertyList.Count();
                string orderGuid;
                foreach (JObject pageJProperty in pageJPropertyList)
                {
                    orderGuid = pageJProperty.SelectToken("Guid").ToObject<string>();
                    if (true || !dataList.Exists(m => m.CheckoutGuid == orderGuid))
                    {
                        Order model = new Order
                        {
                            CheckoutID = pageJProperty.SelectToken("ID").ToObject<string>(),
                            CheckoutGuid = pageJProperty.SelectToken("Guid").ToObject<string>(),
                            OrderState = pageJProperty.SelectToken("State").ToObject<int>() == 0 ? "未恢复" : "已恢复",
                            Email = pageJProperty.SelectToken("Email").ToObject<string>(),
                            CreateTime = pageJProperty.SelectToken("CreateTime").ToObject<DateTime>(),
                            CreateOrderErrorReasonList = new List<string>(0),
                            PayErrorReasonList = new List<string>(0),
                            SessionIDList = new List<string>(0),
                            ESPayTypeList = new List<string>(0),
                            ESCreateOrderResultLogList = new List<string>(0),
                            ESPayResultLogList = new List<string>(0)
                        };
                        await this.UpdateOrderAddress(model);
                        await this.UpdateOrderPayData(totalCount, position, model, 10);
                        dataList.Add(model);
                        position++;

                        workbook = ExcelHelper.CreateOrUpdateWorkbook(dataList);
                        ExcelHelper.SaveWorkbookToFile(workbook, filePath);
                    }
                }
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, $"数据收集遇到异常,正在保存数据，请重新收集...");
            }

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        /// <summary>
        /// 获取国家名称
        /// </summary>
        /// <param name="countryCode"></param>
        /// <returns></returns>
        private async Task<string> GetCountryName(string countryCode)
        {
            string result = null;

            string getData = await this.MemoryCache.GetOrCreateAsync<string>("AllGeography", async cache =>
            {
                string data = (await this.PayHttpClient.Get("https://config.runshopstore.com/api/Geography/GetGeographyAll")).Item2;
                cache.Value = data;
                cache.SlidingExpiration = TimeSpan.FromHours(1);
                return data;
            });

            JArray geographyJArray = JObject.Parse(getData).SelectToken("data").ToObject<JArray>();
            foreach (JObject geographyJObj in geographyJArray)
            {
                if (string.IsNullOrEmpty(geographyJObj.SelectToken("parent_code").ToObject<string>())
                    && geographyJObj.SelectToken("code").ToObject<string>() == countryCode)
                {
                    result = geographyJObj.SelectToken("name").ToObject<string>();
                }
            }
            return result;
        }

        /// <summary>
        /// 更新订单地址数据
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task UpdateOrderAddress(Order model)
        {
            string baseRequestUrl = "https://adoebike.meshopstore.com/api/v1/order/GetGiveUpDetailPageData?id={id}";
            string postUrl = baseRequestUrl.Replace("{id}", model.CheckoutID);

            Dictionary<string, string> heaerDic = new Dictionary<string, string>(3)
            {
                { "Authorization","Bearer 4fLiaKgcZIxrYzUU5rTeiKc61DC44qcHFtC6iDFot23lkSV3nqMLIsbaSi6OgkFwq0Z0O/VUnsZqqIJSXud/G2yqOhpj4zq5mX6tvcyV9Pd9uZg4BmOVMu9L1esGycr6kBizEmLMF2N5VhbauUGDQUr0MYMRwnB6wDU1I6dh+W7EANWerQmCZ2rdcp8wFUWiFMVjRKWTWo3ayygA+YxXRWTW1vYwfciWYouNNXGUtmg+SuWAqAW1N6d53s9FsJAlb125mYxFfDn7beDtR0sHY04yTWnzaI6WyhOqBFZBcuQMJ146JM1M5v2Q5ewyOIWzt/IagSPlWlTV0rczsuqTWvOfW8rguTibhwv4ddDTzMITDLhSwA3vHwqRAxVdBY/x9jtKCQKQXeWqUDvU2qBd2exB4ACy0C1eGB4Ut/tLczj2gIZxyr3wA22NsoaZndTEIs+GNikO2QNrGScr54dDuBFMzNsrKoCBBBV/rO2o87DdA4r4hM6kfNBDzrr/0zZOOtPtmlnIS4eWKkV0Xc394eyNGCu/k3u2VpK1hIvo+MuJX/p+wMAith8ZroQS6Rdkt/IagSPlWlTV0rczsuqTWhS+gdVhKo+H8wrTcIG8VB6ryuPv33nMrltUYQHN8FvavZaufgOrixyF3qwFP0P7ltfjrGqiDjtR9Rim+qTnqXC7EBbWKm2wkBNqlLrWoajHymwOVDgsiSut6rT/HKoW/Pqm5womW7Cj1aHcO/EgxvXmG0RvXJzbTBiKIm6QEosMy9CSJF/ocuIe1ubtnT77n3/D2s2AJn0ripuktavlJsfMmfid/Spkp6ka19rc/HUvNltQKuQ9Oo8xeE2qeQ0OrZ+TSjIrQPmg1r+Qkb16KpTAo9gm0B5X+16e0Ijv7UJeSOC4hMVVx9vOgCAqiLMrWIp6DlqF5NYZE/Ha1r845KExzgW89b2GZ59NbKOauMEMaUC8Re3VnordkFe/J4jGWVd10srS+7ASwDEDmnBrH740OTdN62n93zwc35VMCD6DukQ4QJ8uUuCEVE0jhlCrH0hxb7HqfUTD07l09IhkpMZXEQ7Nrqsc8aTjQJorMcLXHh1yoz23EKE0q9KM38XRFHYNM0J3pdCG2QE/st4FOotkf5TonVSpw7gIUg8lZrNxNYAeQ4MPrfHjCDHAWLggIK35eFDAAhUcp+bMoOsFKuIvX5qAlffNA134N3uojMFB+QQOlWP6aHopnZ1AQ3nSB8xelB9gO7aZr4SmoA45VKYRaQdWhSPzjeR41KUZhcVa"},
                {"access_hash","o/Gy4FobxzC3x7vftVM1Yw==" },
                {"response_in","62652d36" }
            };

            var getResult = await this.PayHttpClient.Get(postUrl, heaerDic);

            JObject checkoutJObj = JObject.Parse(getResult.Item2);
            JArray addressJArray = checkoutJObj.SelectToken("data.AddressList").ToObject<JArray>();
            int addressType = 0;
            string countryCode = null;
            foreach (JObject addressJObj in addressJArray)
            {
                addressType = addressJObj.SelectToken("Type").ToObject<int>();
                countryCode = addressJObj.SelectToken("CountryCode").ToObject<string>();
                if (addressType == 1)
                {
                    model.CountryName = await this.GetCountryName(countryCode);

                    break;
                }
            }

        }

        /// <summary>
        /// 获取支付相关数据
        /// </summary>
        /// <param name="totalCount"></param>
        /// <param name="position"></param>
        /// <param name="model"></param>
        /// <param name="lastDays"></param>
        /// <returns></returns>
        private async Task UpdateOrderPayData(int totalCount, int position, Order model, int lastDays)
        {
            #region 1-获取支付类型

            string dataFilter = @"[
    {
        ""multi_match"": {
            ""type"": ""phrase"",
            ""query"": """ + model.CheckoutGuid + @""",
            ""lenient"": true
        }
    },
    {
        ""bool"": {
            ""should"": [
                {
                    ""multi_match"": {
                        ""type"": ""phrase"",
                        ""query"": ""/ajax/paydd"",
                        ""lenient"": true
                    }
                },
                {
                    ""multi_match"": {
                        ""type"": ""phrase"",
                        ""query"": ""/ajax/pay"",
                        ""lenient"": true
                    }
                }
            ],
            ""minimum_should_match"": 1
        }
    }
]";

            List<ESLog> esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个支付类型数据", dataFilter, lastDays, log =>
            {
                string payType = null;
                if (log.Contains("/ajax/paydd/FPP", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "PayPal快捷";
                }
                else if (log.Contains("/ajax/paydd/PP", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "PayPal";
                }
                else if (log.Contains("/ajax/paydd/PayEaseDirect", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "PayEase直连";
                }
                else if (log.Contains("/ajax/pay/PayEase", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "PayEase三方或者本地化";
                }
                else if (log.Contains("/ajax/paydd/", StringComparison.OrdinalIgnoreCase)
                    || log.Contains("/ajax/pay/", StringComparison.OrdinalIgnoreCase))
                {
                    payType = "其他支付方式+" + log;
                }
                return payType;
            });

            if (esLogList.Count > 0)
            {
                model.ESPayTypeList.AddRange(esLogList.Select(m => m.Type));
            }

            #endregion

            #region 调度版本
            if (model.ESPayType.Contains("PayPal")
                || model.ESPayType.Contains("PayEase直连"))
            {
                #region 2-获取会话ID

                dataFilter = @"[
                            {
                                ""multi_match"": {
                                    ""type"": ""phrase"",
                                    ""query"": """ + model.CheckoutGuid + @""",
                                    ""lenient"": true
                                }
                            },
                            {
                                ""multi_match"": {
                                    ""type"": ""best_fields"",
                                    ""query"": ""token"",
                                    ""lenient"": true
                                }
                            }
                        ]";

                List<string> sessionIDList = new List<string>(10);
                esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个SessionID数据", dataFilter, lastDays, log =>
                {
                    string sessionID = null;
                    if (log.Contains("token", StringComparison.OrdinalIgnoreCase))
                    {
                        sessionID = new Regex("(?<=\"token\":\")[^\"]+(?=\")").Match(log).Value;
                        if (!string.IsNullOrEmpty(sessionID) && !sessionIDList.Contains(sessionID))
                        {
                            sessionIDList.Add(sessionID);
                        }
                    }
                    return sessionID;
                });
                model.SessionIDList.AddRange(sessionIDList);

                #endregion

                #region 获取回话结果日志

                foreach (var sessionID in sessionIDList)
                {
                    dataFilter = @"[
                                {
                                    ""multi_match"": {
                                        ""type"": ""best_fields"",
                                        ""query"": """ + sessionID + @""",
                                        ""lenient"": true
                                    }
                                }
                            ]";
                    esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个创建订单结果数据", dataFilter, lastDays, log =>
                    {
                        string validLog = null;
                        string payError = null;
                        if (log.Contains("CreateOrder_Result", StringComparison.OrdinalIgnoreCase))
                        {
                            //获取创建订单结果日志
                            validLog = log;
                            model.ESCreateOrderResultLogList.Add(validLog);

                            if (model.ESPayType.Contains("PayPal"))
                            {
                                payError = new Regex("(?<=\"issue\":\")[^\"]+(?=\")").Match(log).Value;
                                if (!string.IsNullOrEmpty(payError))
                                {
                                    model.CreateOrderErrorReasonList.Add(payError);
                                }
                            }
                        }
                        else
                        {
                            //获取支付结果日志
                            if (model.ESPayType.Contains("PayPal")
                                && log.Contains("PP_4002_CaptureOrder_Result", StringComparison.OrdinalIgnoreCase))
                            {
                                //获取创建订单结果日志
                                validLog = log;
                                model.ESPayResultLogList.Add(validLog);

                                payError = new Regex("(?<=\"issue\":\")[^\"]+(?=\")").Match(log).Value;
                                if (!string.IsNullOrEmpty(payError))
                                {
                                    model.PayErrorReasonList.Add(payError);
                                }
                            }
                            else if (model.ESPayType.Contains("PayEase直连")
                                && log.Contains("PayEaseDirect_v1Controller_ResultPage", StringComparison.OrdinalIgnoreCase))
                            {
                                //获取创建订单结果日志
                                validLog = log;
                                model.ESPayResultLogList.Add(validLog);

                                payError = new Regex("(?<=\"orderInfo\":\")[^\"]+(?=\")").Match(log).Value;
                                if (!string.IsNullOrEmpty(payError))
                                {
                                    model.PayErrorReasonList.Add(payError);
                                }
                            }
                        }
                        return validLog;
                    });

                }

                #endregion
            }
            #endregion
            #region 非调度版本查询
            else
            {
                #region 2-获取弃单日志列表

                dataFilter = @"[
                            {
                                ""multi_match"": {
                                    ""type"": ""phrase"",
                                    ""query"": """ + model.CheckoutGuid + @""",
                                    ""lenient"": true
                                }
                            }
                        ]";

                esLogList = await this.ESSearchHelper.GetESLogList($"第{position}/{totalCount}个弃单日志数据", dataFilter, lastDays, log =>
                {
                    string validLog = null;
                    string payError = null;

                    //首信易三方
                    if (model.ESPayType.Contains("PayEase三方或者本地化"))
                    {
                        if (log.Contains("PayEase_1002_CreateOrder_Result", StringComparison.OrdinalIgnoreCase))
                        {
                            //获取创建订单结果日志
                            validLog = log;
                            model.ESCreateOrderResultLogList.Add(validLog);

                            payError = new Regex("(?<=\"orderInfo\":\")[^\"]+(?=\")").Match(log).Value;
                            if (!string.IsNullOrEmpty(payError))
                            {
                                model.PayErrorReasonList.Add(payError);
                            }
                        }
                        if (log.Contains("PayEase_1003_CreateOrder_CallBack_Result", StringComparison.OrdinalIgnoreCase))
                        {
                            //获取创建订单结果日志
                            validLog = log;
                            model.ESPayResultLogList.Add(validLog);

                            payError = new Regex("(?<=\"orderInfo\":\")[^\"]+(?=\")").Match(log).Value;
                            if (!string.IsNullOrEmpty(payError))
                            {
                                model.PayErrorReasonList.Add(payError);
                            }
                        }

                    }
                    else if (true)
                    {
                        //...
                    }

                    return validLog;
                });

                #endregion
            }
            #endregion
        }
    }
}
