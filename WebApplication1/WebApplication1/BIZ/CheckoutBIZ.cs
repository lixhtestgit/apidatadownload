using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.Model;

namespace WebApplication1.BIZ
{
    /// <summary>
    /// 弃单业务类
    /// </summary>
    public class CheckoutBIZ
    {
        public AuthBIZ AuthBIZ { get; set; }
        protected HttpClient PayHttpClient { get; set; }
        public IMemoryCache MemoryCache { get; set; }
        public ILogger<CheckoutBIZ> Logger { get; set; }
        public CheckoutBIZ(
            AuthBIZ authBIZ,
            IHttpClientFactory httpClientFactory,
            IMemoryCache memoryCache,
            ILogger<CheckoutBIZ> logger
         )
        {
            this.AuthBIZ = authBIZ;
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.MemoryCache = memoryCache;
            this.Logger = logger;
        }

        /// <summary>
        /// 获取店铺弃单列表
        /// </summary>
        /// <param name="shopUrl"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="beginDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public async Task<List<CheckoutOrder>> GetList(
            string shopUrl,
            string email,
            string password,
            DateTime beginDate,
            DateTime endDate,
            params int[] checkOutSateArray)
        {

            #region 1-获取弃单列表
            string checkoutListUrl = $"https://{shopUrl}/api/v1/order/getgiveuplist";
            Dictionary<string, string> headDic = await this.AuthBIZ.GetShopAuthDic(shopUrl, email, password);

            dynamic filter = null;
            if (checkOutSateArray.Length == 1)
            {
                filter = new
                {
                    CreateTime = new { StartValue = beginDate.AddHours(-8).ToString("yyyy-MM-dd HH:mm:ss"), EndValue = endDate.AddHours(-8).ToString("yyyy-MM-dd HH:mm:ss") },
                    State = new { Value = checkOutSateArray[0] },
                    pager = new { PageNumber = 1, PageSize = 20000 }
                };
            }
            else
            {
                filter = new
                {
                    CreateTime = new { StartValue = beginDate.AddHours(-8).ToString("yyyy-MM-dd HH:mm:ss"), EndValue = endDate.AddHours(-8).ToString("yyyy-MM-dd HH:mm:ss") },
                    pager = new { PageNumber = 1, PageSize = 20000 }
                };
            }
            string shipOrderPostData = JsonConvert.SerializeObject(filter);
            var shipOrderResult = await this.PayHttpClient.Post(checkoutListUrl, shipOrderPostData, headDic);

            JArray checkoutOrderJArray = JObject.Parse(shipOrderResult.Item2).SelectToken("data.Results").ToObject<JArray>() ?? new JArray();
            List<CheckoutOrder> checkoutOrderList = new List<CheckoutOrder>(checkoutOrderJArray.Count);

            foreach (JObject checkoutOrderJObj in checkoutOrderJArray)
            {
                checkoutOrderList.Add(new CheckoutOrder
                {
                    CheckoutID = checkoutOrderJObj.SelectToken("ID").ToObject<string>(),
                    CheckoutGuid = checkoutOrderJObj.SelectToken("Guid").ToObject<string>(),
                    OrderState = checkoutOrderJObj.SelectToken("State").ToObject<int>() == 0 ? "未恢复" : "已恢复",
                    UserName = checkoutOrderJObj.SelectToken("UserName").ToObject<string>(),
                    Email = checkoutOrderJObj.SelectToken("Email").ToObject<string>(),
                    ChoiseCurrency = checkoutOrderJObj.SelectToken("ChoiseCurrency").ToObject<string>(),
                    CurrencyTotalPayPrice = checkoutOrderJObj.SelectToken("CurrencyTotalPayPrice").ToObject<decimal>(),
                    CreateTime = checkoutOrderJObj.SelectToken("CreateTime").ToObject<DateTime>(),
                    ProductPriceList = new List<string>(0),
                    ProductUrlList = new List<string>(2),
                    CreateOrderErrorReasonList = new List<string>(0),
                    PayErrorReasonList = new List<string>(0),
                    SessionIDList = new List<string>(0),
                    ESPayTypeList = new List<string>(0),
                    ESCreateOrderResultLogList = new List<string>(0),
                    ESPayResultLogList = new List<string>(0),
                    CountryName = "无",
                    Province = "无",
                    City = "无",
                    Address = "无",
                    ZIP = "无",
                    Phone = "无"
                });
            }

            int totalCount = checkoutOrderList.Count;
            this.Logger.LogInformation($"共获取未恢复弃单数据{totalCount}个.");

            #endregion

            #region 2-获取弃单详细信息

            int position = 1;
            string baseRequestUrl = $"https://{shopUrl}/api/v1/order/GetGiveUpDetailPageData?id={{id}}";
            checkoutOrderList.ForEach(model =>
            {
                this.Logger.LogInformation($"第{position}/{totalCount}个弃单详细数据正在查询...");

                if (false)
                {
                    string postUrl = baseRequestUrl.Replace("{id}", model.CheckoutID);
                    var getResult = this.PayHttpClient.Get(postUrl, headDic).Result;

                    JObject checkoutJObj = JObject.Parse(getResult.Item2);

                    //获取地址相关数据
                    if (false)
                    {
                        JArray addressJArray = checkoutJObj.SelectToken("data.AddressList").ToObject<JArray>();
                        foreach (JObject addressJObj in addressJArray)
                        {
                            int addressType = addressJObj.SelectToken("Type").ToObject<int>();
                            string countryCode = addressJObj.SelectToken("CountryCode").ToObject<string>();
                            if (addressType == 1)
                            {
                                model.CountryName = this.GetCountryName(countryCode).Result;
                                model.Province = addressJObj.SelectToken("Province").ToObject<string>();
                                model.City = addressJObj.SelectToken("City").ToObject<string>();
                                model.Address = addressJObj.SelectToken("Address1").ToObject<string>() + " " + addressJObj.SelectToken("Address2").ToObject<string>();
                                model.ZIP = addressJObj.SelectToken("ZIP").ToObject<string>();
                                model.Phone = addressJObj.SelectToken("Phone").ToObject<string>();
                                break;
                            }
                        }
                    }

                    //获取产品数据
                    if (false)
                    {
                        JArray productJArray = checkoutJObj.SelectToken("data.ItemList").ToObject<JArray>();
                        foreach (JObject productJObj in productJArray)
                        {
                            string productHref = productJObj.SelectToken("Href").ToObject<string>();
                            if (string.IsNullOrEmpty(productHref))
                            {
                                string productSpuRequestUrl = $"https://{shopUrl}/api/v1/productmanage/product?spuId={productJObj.SelectToken("SPUID")}";
                                var productSpuGetResult = this.PayHttpClient.Get(productSpuRequestUrl, headDic).Result;
                                JObject productSpuJObj = JObject.Parse(productSpuGetResult.Item2);
                                productHref = productSpuJObj.SelectToken("data.Product.Url").ToString();
                            }
                            model.ProductUrlList.Add($"https://{shopUrl}{productHref}");
                            model.ProductPriceList.Add($"{productJObj.SelectToken("Title")}:{productJObj.SelectToken("Price")}");
                        }
                    }
                }

                position++;
            });

            #endregion

            return checkoutOrderList;
        }


        #region 扩展方法

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

        #endregion
    }
}
