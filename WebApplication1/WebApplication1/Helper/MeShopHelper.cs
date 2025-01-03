﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.Enum;
using WebApplication1.Extension;
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
            {"test0003", new { Domain = "runshopstore.com", Email = "tester@meshop.net", Password = "Tester123456" } },
            {"netstore", new { Domain = "meshopstore.com", Email = "chenfei@meshop.net", Password = "Store78sp9" } },
            {"tbdressshop", new { Domain = "meshopstore.com", Email = "chenfei@meshop.net", Password = "Tsh64384hhpo" } },
            {"ericdressfashion", new { Domain = "meshopstore.com", Email = "chenfei@meshop.net", Password = "Chenfei@2022" } },
            {"shoespieshop", new { Domain = "meshopstore.com", Email = "chenfei@meshop.net", Password = "Meshop0823" } },
            {"wigsbuyshop", new { Domain = "meshopstore.com", Email = "chenfei@meshop.net", Password = "Chenfei@2022" } },
            {"janewigshop", new { Domain = "meshopstore.com", Email = "chenfei@meshop.net", Password = "Meshop0823" } },
            {"soomshop", new { Domain = "meshopstore.com", Email = "shpopgo@163.com", Password = "709%sop230" } },
            {"tidebuyshop", new { Domain = "meshopstore.com", Email = "chenfei@meshop.net", Password = "8LAop6734SW02pkl" } },
            {"teamliu5", new { Domain = "meshopstore.com", Email = "sellershop@126.com", Password = "12Meoslp7238nbv" } }
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

        #region 订单

        /// <summary>
        /// 获取订单列表
        /// </summary>
        /// <param name="hostAdmin"></param>
        /// <param name="orderIDS"></param>
        /// <returns></returns>
        public async Task<List<MeShopOrder>> GetOrderList(string hostAdmin, params long[] orderIDS)
        {
            List<MeShopOrder> dataList = null;

            if (hostAdmin.IsNotNullOrEmpty() && orderIDS.Length > 0)
            {
                IDictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);

                string postUrl = $"https://{hostAdmin}.{this.ShopApiV1AuthUserDic[hostAdmin].Domain}/api/v1/order/getorderlist";
                dynamic postData = new
                {
                    ID = new
                    {
                        Value = orderIDS
                    },
                    Pager = new
                    {
                        PageNumber = 1,
                        PageSize = orderIDS.Length
                    }
                };
                string postDataStr = JsonConvert.SerializeObject(postData);


                var postResult = await this.PayHttpClient.PostJson(postUrl, postDataStr, authDic);

                JObject orderJObject = JObject.Parse(postResult.Item2);
                if (orderJObject.SelectToken("success").ToObject<bool>())
                {
                    dataList = orderJObject.SelectToken("data.Results").ToObject<List<MeShopOrder>>();
                }
            }

            return dataList ?? new List<MeShopOrder>(0);
        }

        /// <summary>
        /// 获取订单详情
        /// </summary>
        /// <param name="hostAdmin"></param>
        /// <param name="orderID"></param>
        /// <returns></returns>
        public async Task<MeShopOrderDetail> GetOrderDetail(string hostAdmin, int orderID)
        {
            MeShopOrderDetail shopOrder = null;

            if (hostAdmin.IsNotNullOrEmpty() && orderID > 0)
            {
                Dictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);
                string orderDetailUrl = $"https://{hostAdmin}.{this.ShopApiV1AuthUserDic[hostAdmin].Domain}/api/v1/order/GetOrderDetailPageData?orderID={orderID}";

                var orderDetailResult = await this.PayHttpClient.Get(orderDetailUrl, authDic);

                JObject orderJObject = JObject.Parse(orderDetailResult.Item2);
                if (orderJObject.SelectToken("success").ToObject<bool>())
                {
                    shopOrder = orderJObject.SelectToken("data").ToObject<MeShopOrderDetail>();
                }
            }

            return shopOrder;
        }

        /// <summary>
        /// 获取子单列表
        /// </summary>
        /// <param name="hostAdmin"></param>
        /// <param name="orderIDS"></param>
        /// <returns></returns>
        public async Task<List<MeShopOrderItem>> GetOrderItemList(string hostAdmin, params long[] orderIDS)
        {
            List<MeShopOrderItem> meShopOrderItemList = null;
            if (orderIDS.Length > 0)
            {
                IDictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);
                string postUrl = $"https://{hostAdmin}.{this.ShopApiV1AuthUserDic[hostAdmin].Domain}/api/v1/webuser/importwebuserbysql?isInsert=0";
                string postDataStr = $"select * from order_item where orderid in ({string.Join(',', orderIDS)})";

                var postResult = await this.PayHttpClient.PostText(postUrl, postDataStr, authDic);

                meShopOrderItemList = JsonConvert.DeserializeObject<List<MeShopOrderItem>>(postResult.Item2);
            }

            return meShopOrderItemList ?? new List<MeShopOrderItem>(0);
        }

        /// <summary>
        /// 同步订单发货记录到MeShop
        /// </summary>
        /// <param name="hostAdmin"></param>
        /// <param name="postData"></param>
        /// <returns></returns>
        public async Task<int> SyncOrderShipToShop(string hostAdmin, string postData)
        {
            int result = 0;

            if (hostAdmin.IsNotNullOrEmpty() && postData.IsNotNullOrEmpty())
            {
                Dictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);
                //部分发货
                var syncResult = await this.PayHttpClient.PostJson($"https://{hostAdmin}.{this.ShopApiV1AuthUserDic[hostAdmin].Domain}/api/v1/order/SetShipedState", postData, authDic);

                result = syncResult.Item1 == System.Net.HttpStatusCode.OK ? 1 : 0;
            }

            return result;
        }

        #endregion

        #region 国家省州

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

            return baseGeographyList ?? new List<BaseGeographyModel>(0);
        }

        #endregion

        #region 币种

        /// <summary>
        /// 获取国家省州列表
        /// </summary>
        /// <returns></returns>
        public async Task<List<BaseCurrencyModel>> GetMSCurrency()
        {
            //获取接口基础币种
            List<BaseCurrencyModel> baseCurrencyList = null;
            var getResult = await this.PayHttpClient.Get("https://config.meshopstore.com/api/currency/GetCurrencyAll");

            JObject orderJObject = JObject.Parse(getResult.Item2);

            baseCurrencyList = orderJObject.SelectToken("data").ToObject<List<BaseCurrencyModel>>();

            return baseCurrencyList ?? new List<BaseCurrencyModel>(0);
        }

        #endregion

        #region 产品

        /// <summary>
        /// 获取所有产品列表
        /// </summary>
        /// <param name="hostAdmin"></param>
        /// <returns></returns>
        public async Task<List<MeShopSpuDB>> GetProductList(string hostAdmin)
        {
            string sql = $"select ID,SPU,Handle,State from product_spu where state!=-1";
            List<MeShopSpuDB> dataList = await this.SelectDataToShop<MeShopSpuDB>(hostAdmin, sql);
            return dataList;
        }

        /// <summary>
        /// 获取所有产品列表
        /// </summary>
        /// <param name="hostAdmin"></param>
        /// <param name="createDate"></param>
        /// <returns></returns>
        public async Task<List<MeShopSpuDB>> GetProductListByCreateTime(string hostAdmin, DateTime createDate)
        {
            string sql = $"select ID,SPU,Handle,State from product_spu where state!=-1 and createtime>'{createDate.ToString_yyyyMMddHHmmss()}'";
            List<MeShopSpuDB> dataList = await this.SelectDataToShop<MeShopSpuDB>(hostAdmin, sql);
            return dataList;
        }

        /// <summary>
        /// 获取系列下所有产品列表
        /// </summary>
        /// <param name="hostAdmin"></param>
        /// <param name="collID"></param>
        /// <returns></returns>
        public async Task<List<MeShopSpuDB>> GetProductListByCollID(string hostAdmin, long collID)
        {
            string sql = $"select ID,SPU,Handle,State from product_spu where state!=-1 and ID in (select SPUID from product_collection_product where CollectionID={collID})";
            List<MeShopSpuDB> dataList = await this.SelectDataToShop<MeShopSpuDB>(hostAdmin, sql);
            return dataList;
        }

        /// <summary>
        /// 同步产品状态到MeShop
        /// </summary>
        /// <param name="hostAdmin"></param>
        /// <param name="productState"></param>
        /// <param name="spuIDS"></param>
        /// <returns></returns>
        public async Task<int> SyncProductStateToShop(string hostAdmin, EMeShopProductState productState, params long[] spuIDS)
        {
            int result = 0;

            if (hostAdmin.IsNotNullOrEmpty() && spuIDS.Length > 0)
            {
                Dictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);

                string postData = JsonConvert.SerializeObject(new
                {
                    SpuIds = spuIDS,
                    State = productState
                });

                var syncResult = await this.PayHttpClient.PostJson($"https://{hostAdmin}.{this.ShopApiV1AuthUserDic[hostAdmin].Domain}/api/v1/ProductManage/updateproductstate", postData, authDic);

                result = syncResult.Item1 == System.Net.HttpStatusCode.OK ? 1 : 0;
            }

            return result;
        }

        /// <summary>
        /// 获取产品图片通过SKU
        /// </summary>
        /// <param name="hostAdmin"></param>
        /// <param name="skus"></param>
        /// <returns></returns>
        public async Task<List<MeShopSkuImage>> GetProductImageBySKU(string hostAdmin, params string[] skus)
        {
            skus = skus.ToList().FindAll(m => m.IsNotNullOrEmpty()).Distinct().ToArray();
            for (int i = 0; i < skus.Length; i++)
            {
                skus[i] = $"'{skus[i]}'";
            }
            string inSql = string.Join(",", skus);
            string sql = $"select DISTINCT SKU,SPUID,CONCAT('https://cdn.{this.ShopApiV1AuthUserDic[hostAdmin].Domain}/s/files/{hostAdmin}',(select SRC from product_image where id=product_sku.ImageID)) ImageSrc from product_sku where ImageID>0 and SKU in ({inSql});";
            List<MeShopSkuImage> skuImageList = await this.SelectDataToShop<MeShopSkuImage>(hostAdmin, sql);
            return skuImageList;
        }

        #endregion

        #region 系列

        public async Task<List<MeShopColl>> GetAllColl(string hostAdmin)
        {
            List<MeShopColl> collList = null;
            if (hostAdmin.IsNotNullOrEmpty())
            {
                Dictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);
                string postDataStr = $"select ID,Title,Type from product_collection where State=1";

                string postUrl = $"https://{hostAdmin}.{this.ShopApiV1AuthUserDic[hostAdmin].Domain}/api/v1/webuser/importwebuserbysql?isInsert=0";
                var postResult = await this.PayHttpClient.PostText(postUrl, postDataStr, authDic);

                collList = JsonConvert.DeserializeObject<List<MeShopColl>>(postResult.Item2);
            }
            return collList ?? new List<MeShopColl>(0);
        }

        /// <summary>
        /// 删除系列下所有产品系列关系
        /// </summary>
        /// <param name="hostAdmin"></param>
        /// <param name="productIDS"></param>
        /// <returns></returns>
        public async Task<int> DeleteCollProductByCollID(string hostAdmin, long collID)
        {
            int result = 0;

            if (hostAdmin.IsNotNullOrEmpty() && collID > 0)
            {
                string updateSql = null;
                try
                {
                    Dictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);
                    updateSql = $"delete from product_collection_product where CollectionID in ({collID})";
                    var syncResult = await this.PayHttpClient.PostText($"https://{hostAdmin}.{this.ShopApiV1AuthUserDic[hostAdmin].Domain}/api/v1/webuser/importwebuserbysql?isInsert=1", updateSql, authDic);

                    result = Convert.ToInt32(syncResult.Item2);
                }
                catch (Exception e)
                {
                    this.Logger.LogError(e, $"执行SQL异常,hostAdmin={hostAdmin},syncSql={updateSql}");
                    throw;
                }
            }

            return result;
        }

        /// <summary>
        /// 删除产品下所有的产品系列关系
        /// </summary>
        /// <param name="hostAdmin"></param>
        /// <param name="productIDS"></param>
        /// <returns></returns>
        public async Task<int> DeleteCollProductByProductID(string hostAdmin, params long[] productIDS)
        {
            int result = 0;

            if (hostAdmin.IsNotNullOrEmpty() && productIDS.Length > 0)
            {
                string updateSql = null;
                try
                {
                    Dictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);
                    string inSql = string.Join(',', productIDS);
                    updateSql = $"delete from product_collection_product where SPUID in ({inSql})";
                    var syncResult = await this.PayHttpClient.PostText($"https://{hostAdmin}.{this.ShopApiV1AuthUserDic[hostAdmin].Domain}/api/v1/webuser/importwebuserbysql?isInsert=1", updateSql, authDic);

                    result = Convert.ToInt32(syncResult.Item2);
                }
                catch (Exception e)
                {
                    this.Logger.LogError(e, $"执行SQL异常,hostAdmin={hostAdmin},syncSql={updateSql}");
                    throw;
                }
            }

            return result;
        }

        /// <summary>
        /// 添加系列下产品（支持已添加过当前系列的产品）
        /// </summary>
        /// <param name="hostAdmin"></param>
        /// <param name="collID"></param>
        /// <param name="spuIDS"></param>
        /// <returns></returns>
        public async Task<int> AddCollProduct(string hostAdmin, long collID, params long[] spuIDS)
        {
            int result = 0;

            if (hostAdmin.IsNotNullOrEmpty() && collID > 0 && spuIDS.Length > 0)
            {
                Dictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);

                string postData = JsonConvert.SerializeObject(new
                {
                    CollectionID = collID,
                    CheckSpuIDs = spuIDS,
                    OprateSpuIDs = new long[0]
                });
                var syncResult = await this.PayHttpClient.PostJson($"https://{hostAdmin}.{this.ShopApiV1AuthUserDic[hostAdmin].Domain}/api/v1/Collection/oprateproduct", postData, authDic);

                result = syncResult.Item1 == System.Net.HttpStatusCode.OK ? 1 : 0;
            }

            return result;
        }


        #endregion

        #region 用户

        public async Task<MeShopUser> GetUser(string hostAdmin, string email)
        {
            string sql = $"select * from user_info where email='{email}'";

            List<MeShopUser> userList = await this.SelectDataToShop<MeShopUser>(hostAdmin, sql);
            return userList.FirstOrDefault();
        }

        #endregion

        #region Sql

        /// <summary>
        /// 同步用户到MeShop
        /// </summary>
        /// <param name="hostAdmin"></param>
        /// <param name="syncSql"></param>
        /// <returns></returns>
        public async Task<int> ExecSqlToShop(string hostAdmin, string syncSql)
        {
            int result = 0;

            if (hostAdmin.IsNotNullOrEmpty() && syncSql.IsNotNullOrEmpty())
            {
                try
                {
                    Dictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);
                    var syncResult = await this.PayHttpClient.PostText($"https://{hostAdmin}.{this.ShopApiV1AuthUserDic[hostAdmin].Domain}/api/v1/webuser/importwebuserbysql?isInsert=1", syncSql, authDic);

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

        public async Task<List<T>> SelectDataToShop<T>(string hostAdmin, string syncSql)
        {
            List<T> tList = null;
            if (hostAdmin.IsNotNullOrEmpty() && syncSql.IsNotNullOrEmpty())
            {
                Dictionary<string, string> authDic = await this.GetAuthDic(hostAdmin);
                var syncResult = await this.PayHttpClient.PostText($"https://{hostAdmin}.{this.ShopApiV1AuthUserDic[hostAdmin].Domain}/api/v1/webuser/importwebuserbysql?isInsert=0", syncSql, authDic);

                tList = JArray.Parse(syncResult.Item2).ToObject<List<T>>();
            }

            return tList ?? new List<T>(0);
        }

        #endregion

    }
}
