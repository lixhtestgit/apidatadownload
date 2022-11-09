using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebApplication1.Helper
{
    public class MeShopHelper
    {
        protected HttpClient PayHttpClient { get; set; }
        public ILogger Logger { get; set; }

        public MeShopHelper(IHttpClientFactory httpClientFactory, ILogger<MeShopHelper> logger)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.Logger = logger;
        }

        public Dictionary<string, Dictionary<string, string>> ShopAuthDic = new Dictionary<string, Dictionary<string, string>>(100);

        /// <summary>
        /// 获取店铺授权字典
        /// </summary>
        /// <param name="hostAdmin">店铺名</param>
        /// <returns></returns>
        public async Task<Dictionary<string, string>> GetAuthDic(string hostAdmin)
        {
            Dictionary<string, string> authDic = null;
            if (this.ShopAuthDic.ContainsKey(hostAdmin))
            {
                authDic = this.ShopAuthDic[hostAdmin];
            }
            else
            {
                string loginEmail = "chenfei@meshop.net";
                string loginPwd = "Meshop123";

                if (hostAdmin == "pepperfry-outlet-store")
                {
                    loginEmail = "chenfei@meshop.net";
                    loginPwd = "Bl123456";
                }

                string authUrl = "https://sso.meshopstore.com/Auth/Login";
                var loginResult = await this.PayHttpClient.Post(authUrl, postDict: new Dictionary<string, string>
                {
                    {"email",loginEmail },
                    {"password",loginPwd },
                    {"shopUrl",hostAdmin }
                }, null);

                authDic = new Dictionary<string, string>(3);
                JObject authJObject = JObject.Parse(loginResult.Item2);
                authDic.Add("Authorization", "Bearer " + authJObject.SelectToken("access_token").ToString());
                authDic.Add("access_hash", authJObject.SelectToken("access_hash").ToString());
                authDic.Add("response_in", authJObject.SelectToken("response_in").ToString());

                this.ShopAuthDic.Add(hostAdmin, authDic);
            }

            return authDic ?? new Dictionary<string, string>(0);
        }

        public async Task<MeShopOrder> GetOrder(string hostAdmin, int orderID)
        {
            MeShopOrder shopOrder = null;

            string orderDetailUrl = $"https://{hostAdmin}.meshopstore.com/api/v1/order/GetOrderDetailPageData?orderID={orderID}";
            Dictionary<string, string> authDict = await this.GetAuthDic(hostAdmin);
            var orderDetailResult = await this.PayHttpClient.Get(orderDetailUrl, authDict);

            JObject orderJObject = JObject.Parse(orderDetailResult.Item2);
            if (orderJObject.SelectToken("success").ToObject<bool>())
            {
                shopOrder = orderJObject.SelectToken("data").ToObject<MeShopOrder>();
            }

            return shopOrder;
        }
    }

    /// <summary>
    /// 店铺订单
    /// </summary>
    public class MeShopOrder
    {
        public int ID { get; set; }
        public DateTime CreateTime { get; set; }
        public string Email { get; set; }
        public int State { get; set; }
        public decimal CurrencyTotalPayPrice { get; set; }
        public decimal ShipPrice { get; set; }

        public List<MeShopOrderDetail> OrderItemList { get; set; }
    }

    public class MeShopOrderDetail
    {
        public int ID { get; set; }
        public decimal SplitPayPrice { get; set; }
    }
}
