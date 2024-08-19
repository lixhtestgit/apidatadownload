using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebApplication1.Helper
{
    public class MeShopNewHelper
    {
        protected HttpClient PayHttpClient { get; set; }
        public ILogger Logger { get; set; }

        public MeShopNewHelper(IHttpClientFactory httpClientFactory, ILogger<MeShopHelper> logger)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.Logger = logger;
        }

        /// <summary>
        /// 店铺A2接口授权字典
        /// </summary>
        public Dictionary<string, string> ShopApiA2AuthDic = new Dictionary<string, string>
        {
            {"tbdressshop","MYI6mMH+GzJcSKPeBGZe1OF/kQo1MxqsB3RpBTgf/qNvMdDgflJGHz+woOA2/IlyNQy8HS4WX1uKbs1IS0o7CsGewruPCYMDhqquUgOe+asfEfibwqfTng+R+qwzI7FrxEWp1YgY5miNQrIlgFRiwUBt7PaYJC22wMpTZOxB0hTgrmikvj4Hh+you1G7jclgl29gCzoyfX4E4v1u7Nok4o6A5bHGith4JXWBAwRDtYL6p+zszEIUlEM0CnhdMs83Peq5Br0rlFWVMuOKP+dseXVqai7SJh/LgViIyfpdZWfZv3DQggnARPJfJx5gkI4chemzN3Er9Vap/w3X//iptUMjut4lDHqFPZUzilVqgSL8I38zVkbWfXQIJdo6RAxnJ90AzY5cNIktyxdWbTgOo4LuAtW4vuxDFoc4CnLh2lwlCTUu4cbaX0IAzVbCXF6J71H6EdfNHl3gnB/FExB4RbQ8hO8TdwXy0NI2swWBY0w=" },
            {"ericdressfashion","rQ5nlVY72xup/Cprou9pV1NyHOMvsRD/NcTVg+ML9IbtSKH2rvKSi+VNY8A9LlGQt81Vxr/WDCPS0hyQYwj3HVc1oHY03L0n7EzMpCPOix4hFlKExNaStLrcjQ3emDbQcyijq/1bZ9h0Ty1Euk9ncfJSa2JT9Ygj4wsEstH/vvIj4z7TogSH/+Y40nKWdLJ42+6dD2RrCVHLfgIp5svTB4JV1J+Dnn8ujZ2eYxkaO+TNVpwWEf5FJHZ7Qug/r+mqO9td8QvVxQyGmQZ8JeGfy5VMZuwhKic3/q+rbXjnC4bcFiAMww6zH6FTDkkAk6Mvr0pcZailv7d9HLivfRUIe5vT+GBAqvVQwCAERBeheo9/eI7elccAw1128ItJfO3UtzC8Ies0DuoxbV0s2i1Ixjgd2v/klEuyIjWFnTQYOLqyL4k/aPwTn2KG5nIcXSxCv4zKDNSR9t6kGBvS/FUNqWi1Udt7Ughvjb6KKv52oFM=" },
            {"wigsbuyshop","RyF1R7YB+CBbHktxOWTXNVNV3/upY3jOyofYeAboDk1MC37zghQsMwTS/bXI2f/IBWy5qYGrijRpN3UWXqtl8WofAl2tsIdQtpKJR90ok4a9B7UjYMHYErpe4f94kSkIECPU4JvU4kY8Zaqb7mXlMe8QHBFebkse29YfYonzPEgoLHnh85p7tnv57l7b18doe/jAiIXPa4HHij7a5rS5s3B4avieQDuC/vyPj87+1aJwBYXhnpuRWg6DHTe1ejgQ8O6uuga3fRkFHa1Qp+0CA4kOSURUOQ5BC0GvUd/zJoN3fiydO+I/73Ni4xm8eiwEH+Nmh+DYSH9hCvZPTwsxenJxFAlOZyaCXLd/FbeMfHY3lvHnYUcv2Hj1gCZxjpLk5gPFo6uxrqbXH3QNO1BBU6Yj7GqOkBI0qmI0gOjyNAk5NuwPtUAEZ+tw9nNFRIkp8+mQz7FqcvNu63cO2QY9LRwH5Glf9coRzXexZwbfwOs=" }
        };

        public Dictionary<string, Dictionary<string, string>> ShopApiV1AuthDic = new Dictionary<string, Dictionary<string, string>>(100);
        /// <summary>
        /// 店铺V1授权用户字典
        /// </summary>
        public Dictionary<string, dynamic> ShopApiV1AuthUserDic = new Dictionary<string, dynamic>
        {
            {"meshop001", new { Domain = "runshopstore.com", Email = "tester@meshop.net", Password = "Tester123456" } },
        };

        #region 授权

        /// <summary>
        /// 获取店铺授权字典
        /// </summary>
        /// <param name="hostAdmin">店铺名</param>
        /// <returns></returns>
        public async Task<Dictionary<string, string>> GetAuthDic(string hostAdmin)
        {
            Dictionary<string, string> authDic = null;
            if (this.ShopApiV1AuthDic.ContainsKey(hostAdmin))
            {
                authDic = this.ShopApiV1AuthDic[hostAdmin];
            }
            else
            {
                string domain = this.ShopApiV1AuthUserDic[hostAdmin].Domain;
                string loginEmail = this.ShopApiV1AuthUserDic[hostAdmin].Email;
                string loginPwd = this.ShopApiV1AuthUserDic[hostAdmin].Password;

                string authUrl = $"https://sso.{domain}/Auth/Login";
                var loginResult = await this.PayHttpClient.PostForm(authUrl, postDict: new Dictionary<string, string>
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

                this.ShopApiV1AuthDic.Add(hostAdmin, authDic);
            }

            return authDic ?? new Dictionary<string, string>(0);
        }

        #endregion

        #region Sql

        /// <summary>
        /// 同步用户到MeShop
        /// </summary>
        /// <param name="hostAdmin"></param>
        /// <param name="syncSql"></param>
        /// <returns></returns>
        public async Task<int> ExecSqlToShop(string hostAdmin, int isShopDB, string syncSql)
        {
            int result = 0;

            if (hostAdmin.IsNotNullOrEmpty() && syncSql.IsNotNullOrEmpty())
            {
                try
                {
                    //Dictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);
                    Dictionary<string, string> authDic = null;
                    var syncResult = await this.PayHttpClient.PostText($"http://localhost:5555/api/userinfo/importwebuserbysql?signKey=3075b02398744c14a913d47f73d2e621&isShopDB={isShopDB}&isInsert=1", syncSql, authDic);

                    result = Convert.ToInt32(syncResult.Item2);
                }
                catch (Exception e)
                {
                    this.Logger.LogError(e, $"执行SQL异常,hostAdmin={hostAdmin},syncSql={syncSql}");
                    throw;
                }
            }

            return result;
        }

        public async Task<List<T>> SelectDataToShop<T>(string hostAdmin,int isShopDB, string syncSql)
        {
            List<T> tList = null;
            if (hostAdmin.IsNotNullOrEmpty() && syncSql.IsNotNullOrEmpty())
            {
                //Dictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);
                Dictionary<string, string> authDic = null;
                var syncResult = await this.PayHttpClient.PostText($"https://{hostAdmin}.{this.ShopApiV1AuthUserDic[hostAdmin].Domain}/api/userinfo/importwebuserbysql?signKey=3075b02398744c14a913d47f73d2e621&isShopDB={isShopDB}&isInsert=0", syncSql, authDic);

                tList = JArray.Parse(syncResult.Item2).ToObject<List<T>>();
            }

            return tList ?? new List<T>(0);
        }

        #endregion

    }
}
