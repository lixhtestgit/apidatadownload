using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql.Internal.TypeHandlers;
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
            {"tbdressshop","MYI6mMH+GzJcSKPeBGZe1OF/kQo1MxqsB3RpBTgf/qNvMdDgflJGHz+woOA2/IlyNQy8HS4WX1uKbs1IS0o7CsGewruPCYMDhqquUgOe+asfEfibwqfTng+R+qwzI7FrxEWp1YgY5miNQrIlgFRiwUBt7PaYJC22wMpTZOxB0hTgrmikvj4Hh+you1G7jclgl29gCzoyfX4E4v1u7Nok4o6A5bHGith4JXWBAwRDtYL6p+zszEIUlEM0CnhdMs83Peq5Br0rlFWVMuOKP+dseXVqai7SJh/LgViIyfpdZWfZv3DQggnARPJfJx5gkI4chemzN3Er9Vap/w3X//iptUMjut4lDHqFPZUzilVqgSL8I38zVkbWfXQIJdo6RAxnJ90AzY5cNIktyxdWbTgOo4LuAtW4vuxDFoc4CnLh2lwlCTUu4cbaX0IAzVbCXF6J71H6EdfNHl3gnB/FExB4RbQ8hO8TdwXy0NI2swWBY0w=" }
        };

        /// <summary>
        /// 店铺A2接口授权字典
        /// </summary>
        public Dictionary<string, string> ShopApiV1AuthDic = new Dictionary<string, string>
        {
            {"thepowers","eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6ImNoZW5mZWlAbWVzaG9wLm5ldCIsInBob25lX251bWJlciI6IiIsIm5hbWUiOiJjaGVuIGZlaSIsImlkIjoiMTEyIiwicm9sZSI6IjEiLCJzaG9wX2lkIjoiNzciLCJhZGRyZXNzZXMiOiJ0aGVwb3dlcnMiLCJzY29wZXMiOiIiLCJuYmYiOjE3MzIwMjk0MjAsImV4cCI6MTczMjA2NTQyMCwiaXNzIjoiTWVTaG9wLkFQSS5Jc3N1ZXIiLCJhdWQiOiJNZVNob3AuQVBJLkF1ZGllbmNlIn0.Brl5wCp2efmNx5dan09p7QuQ77F6URmO3E5uInCO56A" }
        };

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
                    var syncResult = await this.PayHttpClient.PostText($"https://{hostAdmin}.mestoresy.com/api/userinfo/importwebuserbysql?signKey={DateTime.UtcNow.ToString("MM")}_3075b02398744c14a913d47f73d2e621_{DateTime.UtcNow.ToString("dd")}&isShopDB={isShopDB}&isInsert=1", syncSql, authDic);

                    result = JObject.Parse(syncResult.Item2).SelectToken("data").ToObject<int>();
                }
                catch (Exception e)
                {
                    this.Logger.LogError(e, $"执行SQL异常,hostAdmin={hostAdmin},syncSql={syncSql}");
                    throw;
                }
            }

            return result;
        }

        public async Task<List<T>> SelectDataToShop<T>(string hostAdmin, int isShopDB, string syncSql)
        {
            List<T> tList = null;
            if (hostAdmin.IsNotNullOrEmpty() && syncSql.IsNotNullOrEmpty())
            {
                //Dictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);
                Dictionary<string, string> authDic = null;
                var syncResult = await this.PayHttpClient.PostText($"https://{hostAdmin}.mestoresy.com/api/userinfo/importwebuserbysql?signKey={DateTime.UtcNow.ToString("MM")}_3075b02398744c14a913d47f73d2e621_{DateTime.UtcNow.ToString("dd")}&isShopDB={isShopDB}&isInsert=0", syncSql, authDic);

                tList = JArray.Parse(syncResult.Item2).ToObject<List<T>>();
            }

            return tList ?? new List<T>(0);
        }

        #endregion

        #region 产品

        public async Task<bool> SyncProductDataToES(string hostAdmin, List<string> spuList)
        {
            bool result = false;
            if (hostAdmin.IsNotNullOrEmpty() && spuList.Count > 0)
            {
                Dictionary<string, string> authDic = new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {this.ShopApiV1AuthDic[hostAdmin]}" }
                };
                var syncResult = await this.PayHttpClient.PostJson($"https://{hostAdmin}.mestoresy.com/api/product/testsyncdatas", JsonConvert.SerializeObject(spuList), authDic);

                result = JObject.Parse(syncResult.Item2).SelectToken("data").ToObject<bool>();
            }

            return result;
        }

        #endregion
    }
}
