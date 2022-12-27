using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.Model.MeShop;

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

		public Dictionary<string, Dictionary<string, string>> ShopApiV1AuthDic = new Dictionary<string, Dictionary<string, string>>(100);
		public Dictionary<string, string> ShopApiA2AuthDic = new Dictionary<string, string>
		{
			{"tbdressshop","MYI6mMH+GzJcSKPeBGZe1OF/kQo1MxqsB3RpBTgf/qNvMdDgflJGHz+woOA2/IlyNQy8HS4WX1uKbs1IS0o7CsGewruPCYMDhqquUgOe+asfEfibwqfTng+R+qwzI7FrxEWp1YgY5miNQrIlgFRiwUBt7PaYJC22wMpTZOxB0hTgrmikvj4Hh+you1G7jclgl29gCzoyfX4E4v1u7Nok4o6A5bHGith4JXWBAwRDtYL6p+zszEIUlEM0CnhdMs83Peq5Br0rlFWVMuOKP+dseXVqai7SJh/LgViIyfpdZWfZv3DQggnARPJfJx5gkI4chemzN3Er9Vap/w3X//iptUMjut4lDHqFPZUzilVqgSL8I38zVkbWfXQIJdo6RAxnJ90AzY5cNIktyxdWbTgOo4LuAtW4vuxDFoc4CnLh2lwlCTUu4cbaX0IAzVbCXF6J71H6EdfNHl3gnB/FExB4RbQ8hO8TdwXy0NI2swWBY0w=" },
			{"ericdressfashion","rQ5nlVY72xup/Cprou9pV1NyHOMvsRD/NcTVg+ML9IbtSKH2rvKSi+VNY8A9LlGQt81Vxr/WDCPS0hyQYwj3HVc1oHY03L0n7EzMpCPOix4hFlKExNaStLrcjQ3emDbQcyijq/1bZ9h0Ty1Euk9ncfJSa2JT9Ygj4wsEstH/vvIj4z7TogSH/+Y40nKWdLJ42+6dD2RrCVHLfgIp5svTB4JV1J+Dnn8ujZ2eYxkaO+TNVpwWEf5FJHZ7Qug/r+mqO9td8QvVxQyGmQZ8JeGfy5VMZuwhKic3/q+rbXjnC4bcFiAMww6zH6FTDkkAk6Mvr0pcZailv7d9HLivfRUIe5vT+GBAqvVQwCAERBeheo9/eI7elccAw1128ItJfO3UtzC8Ies0DuoxbV0s2i1Ixjgd2v/klEuyIjWFnTQYOLqyL4k/aPwTn2KG5nIcXSxCv4zKDNSR9t6kGBvS/FUNqWi1Udt7Ughvjb6KKv52oFM=" },
			{"wigsbuyshop","RyF1R7YB+CBbHktxOWTXNVNV3/upY3jOyofYeAboDk1MC37zghQsMwTS/bXI2f/IBWy5qYGrijRpN3UWXqtl8WofAl2tsIdQtpKJR90ok4a9B7UjYMHYErpe4f94kSkIECPU4JvU4kY8Zaqb7mXlMe8QHBFebkse29YfYonzPEgoLHnh85p7tnv57l7b18doe/jAiIXPa4HHij7a5rS5s3B4avieQDuC/vyPj87+1aJwBYXhnpuRWg6DHTe1ejgQ8O6uuga3fRkFHa1Qp+0CA4kOSURUOQ5BC0GvUd/zJoN3fiydO+I/73Ni4xm8eiwEH+Nmh+DYSH9hCvZPTwsxenJxFAlOZyaCXLd/FbeMfHY3lvHnYUcv2Hj1gCZxjpLk5gPFo6uxrqbXH3QNO1BBU6Yj7GqOkBI0qmI0gOjyNAk5NuwPtUAEZ+tw9nNFRIkp8+mQz7FqcvNu63cO2QY9LRwH5Glf9coRzXexZwbfwOs=" }
		};

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
				string loginEmail = "chenfei@meshop.net";
				string loginPwd = "Chenfei@2022";

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

				this.ShopApiV1AuthDic.Add(hostAdmin, authDic);
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

		/// <summary>
		/// 获取国家省州列表
		/// </summary>
		/// <returns></returns>
		public async Task<List<BaseGeographyModel>> GetMSGeography()
		{
			//获取接口基础币种
			List<BaseGeographyModel> baseGeographyList = null;
			var getResult = await this.PayHttpClient.Get("https://config.runshopstore.com/api/Geography/GetGeographyAll");

			JObject orderJObject = JObject.Parse(getResult.Item2);

			baseGeographyList = orderJObject.SelectToken("data").ToObject<List<BaseGeographyModel>>();

			return baseGeographyList;
		}

		public async Task<int> SyncUserToShop(string hostAdmin, string syncSql)
		{
			int result = 0;

			Dictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);
			var syncResult = await this.PayHttpClient.Post($"https://{hostAdmin}.meshopstore.com/api/v1/webuser/importwebuserbysql?isInsert=1", syncSql, authDic, "text/plain");

			result = Convert.ToInt32(syncResult.Item2);
			return result;
		}

		public async Task<JArray> SelectUserToShop(string hostAdmin, string syncSql)
		{
			Dictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);
			var syncResult = await this.PayHttpClient.Post($"https://{hostAdmin}.meshopstore.com/api/v1/webuser/importwebuserbysql?isInsert=0", syncSql, authDic, "text/plain");

			JArray result = JArray.Parse(syncResult.Item2);
			return result;
		}
	}
}
