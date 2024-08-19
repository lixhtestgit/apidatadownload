using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyPaymentHelper.Helper
{
    public class DDHelper
    {
        private readonly ILogger<DDHelper> logger;
        private IMemoryCache _memoryCache;
        private readonly IConfiguration configuration;
        private HttpClient _httpClient;

        private Dictionary<string, string> jqAuthDic = new Dictionary<string, string>();

        public DDHelper(
            ILogger<DDHelper> logger,
            IMemoryCache memoryCache,
            IConfiguration configuration)
        {
            this.logger = logger;
            this._memoryCache = memoryCache;
            this.configuration = configuration;

            WebProxy proxy = new WebProxy("http://127.0.0.1:10088");
            HttpClientHandler handler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true
            };
            this._httpClient = new HttpClient(handler);

            this._httpClient.Timeout = TimeSpan.FromSeconds(20);

            if (this.jqAuthDic == null || this.jqAuthDic.Count == 0)
            {
                this.initJQAuthCookie();
            }
        }

        private void initJQAuthCookie()
        {
            string[] jqArray = new string[] { "meshopstore.com", "mystoresz.com" };
            foreach (string jq in jqArray)
            {
                string authCookie = null;
                string postUrl = $"https://dd.{jq}/adm/auth/logindo";
                string postData = JsonConvert.SerializeObject(new
                {
                    account = "admin",
                    password = "iSHZdxbyWQqfbNT8a78WF392qZ8vJTa7WRoe9wcWJ2v2sUvWfNY4QP2YpvPN75Hd6sL5rYE8SK2FpZB2BjYoV2f6RC2R8VRzxZYN"
                });

                try
                {
                    var loginResult = this._httpClient.PostJsonGetHeaderDic(postUrl, postData).ConfigureAwait(false).GetAwaiter().GetResult();
                    authCookie = loginResult.Item3["Set-Cookie"][0];
                }
                catch (Exception e)
                {
                    this.logger.LogError(e, $"获取调度授权失败,detail:{{postUrl={postUrl},postData={postData}}}");
                    throw;
                }

                this.jqAuthDic.Add(jq, authCookie);
            }
        }

        public async Task<Dictionary<string, List<DDRuleList>>> GetRuleDic(string jq, string groupByPayTypeOrSiteCode = "payType")
        {
            var getListResult = await this._httpClient.Get($"https://dd.{jq}/adm/rule/getlist?page=1&limit=999", new Dictionary<string, string>
            {
                {"Cookie", this.jqAuthDic[jq] }
            });

            List<DDRuleList> ruleList = JsonConvert.DeserializeObject<JObject>(getListResult.Item2).SelectToken("data").ToObject<List<DDRuleList>>() ?? new List<DDRuleList>(0);

            Dictionary<string, List<DDRuleList>> ruleDic = new Dictionary<string, List<DDRuleList>>(20);
            string groupName = null;
            foreach (var rule in ruleList)
            {
                if (string.IsNullOrEmpty(rule.Rule.SiteCode))
                {
                    rule.Rule.SiteCode = "全部";
                }
                if (string.IsNullOrEmpty(rule.Rule.PaymentMethod))
                {
                    rule.Rule.PaymentMethod = "全部";
                }
                rule.Rule.CreateDateTime = rule.Rule.CreateDateTime.AddHours(8);
                if (groupByPayTypeOrSiteCode == "payType")
                {
                    groupName = rule.Rule.PaymentMethod;
                }
                else if (groupByPayTypeOrSiteCode == "siteCode")
                {
                    groupName = rule.Rule.SiteCode;
                }

                if (ruleDic.ContainsKey(groupName))
                {
                    ruleDic[groupName].Add(rule);
                }
                else
                {
                    ruleDic.Add(groupName, new List<DDRuleList> { rule });
                }
            }
            return ruleDic;
        }

        public async Task<bool> DelRuleDic(string jq, string ruleId)
        {
            if (string.IsNullOrEmpty(ruleId) == false)
            {
                var getListResult = await this._httpClient.PostJsonGetHeaderDic($"https://dd.{jq}/adm/rule/removedo",
                    JsonConvert.SerializeObject(new
                    {
                        Ids = new string[] { ruleId }
                    }),
                    new Dictionary<string, string>
                    {
                        {"Cookie", this.jqAuthDic[jq] }
                    }
                );
                return getListResult.Item1 == System.Net.HttpStatusCode.OK;
            }
            return false;
        }

        public async Task<List<DDPay>> GetPayListAsync(string jq)
        {
            var getPayResult = await this._httpClient.Get($"https://dd.{jq}/adm/pay/list?page=1&limit=999", new Dictionary<string, string>
            {
                {"Cookie", this.jqAuthDic[jq] }
            });
            List<DDPay> ddPayList = JObject.Parse(getPayResult.Item2).SelectToken("data").ToObject<List<DDPay>>();
            return ddPayList ?? new List<DDPay>(0);
        }

        public async Task<List<DDPayAccount>> GetPayAccountListAsync(string jq)
        {
            List<DDPayAccount> payAccountList = await this._memoryCache.GetOrCreateAsync($"ddPayAccount_{jq}", async cacheEntry =>
            {
                var getAccountResult = await this._httpClient.Get($"https://dd.{jq}/adm/payAccount/pagedlistdo?page=1&limit=999", new Dictionary<string, string>
                {
                    {"Cookie", this.jqAuthDic[jq] }
                });

                List<DDPayAccount> payAccountList = JObject.Parse(getAccountResult.Item2).SelectToken("data").ToObject<List<DDPayAccount>>();

                cacheEntry.Value = payAccountList;
                cacheEntry.AbsoluteExpiration = DateTimeOffset.Now.AddHours(1);
                return payAccountList;
            });
            return payAccountList ?? new List<DDPayAccount>(0);
        }
        public async Task<DDPayAccount> GetPayAccountAsync(string jq, string accountName)
        {
            List<DDPayAccount> payAccountList = await this.GetPayAccountListAsync(jq);
            DDPayAccount payAccount = payAccountList.FirstOrDefault(m => m.Name.Equals(accountName, System.StringComparison.OrdinalIgnoreCase));
            return payAccount;
        }
        public async Task<bool> DelPayAccountAsync(string jq, params string[] accountIdArray)
        {
            if (accountIdArray.Length > 0)
            {
                var delAccountResult = await this._httpClient.PostJsonGetHeaderDic($"https://dd.{jq}/adm/payaccount/removedo", JsonConvert.SerializeObject(new
                {
                    Ids = accountIdArray
                }), new Dictionary<string, string>
                {
                    {"Cookie", this.jqAuthDic[jq] }
                });
                return delAccountResult.Item1 == System.Net.HttpStatusCode.OK;
            }
            return false;
        }
        public async Task<(string, string)> CheckOrAddPayAccount(string jq, string payType, string accountName, string accountConfig)
        {
            string payAccountId = null;
            string remark = null;

            DDPayAccount payAccount = await this.GetPayAccountAsync(jq, accountName);
            if (payAccount != null)
            {
                payAccountId = payAccount.Id;
                remark = $"支付账号：{accountName}信息已存在，如需更新请先删除账号。";
            }
            else
            {
                var addAccountResult = await this._httpClient.PostJson($"https://dd.{jq}/adm/payaccount/adddo", JsonConvert.SerializeObject(new
                {
                    name = accountName,
                    paymentMethod = payType,
                    status = 1,
                    accountConfig = JObject.Parse(accountConfig)
                }), new Dictionary<string, string>
                {
                    {"Cookie", this.jqAuthDic[jq] }
                });
                if (addAccountResult.Item1 == System.Net.HttpStatusCode.OK)
                {
                    //移除支付账号列表缓存
                    this._memoryCache.Remove($"ddPayAccount_{jq}");
                    payAccount = await this.GetPayAccountAsync(jq, accountName);
                    payAccountId = payAccount?.Id ?? "";
                    if (string.IsNullOrEmpty(payAccountId))
                    {
                        remark += $"添加支付账号成功，但获取账号信息失败！detail:{{accountName:{accountName}}}。";
                    }
                }
                else
                {
                    remark += $"添加支付账号失败！detail:{{accountName:{accountName},response:{JsonConvert.SerializeObject(addAccountResult)}}}。";
                }
            }

            return (payAccountId, remark);
        }

        /// <summary>
        /// 添加基础规则
        /// </summary>
        /// <param name="jq"></param>
        /// <param name="clusterCode"></param>
        /// <param name="payId"></param>
        /// <param name="payType"></param>
        /// <param name="siteCodeList"></param>
        /// <param name="accountName"></param>
        /// <param name="accountConfig"></param>
        /// <returns></returns>
        public async Task<(bool, string)> AddBaseRuleDic(string jq, string clusterCode, string payId, string payType, List<string> siteCodeList, string accountName, string accountConfig)
        {
            bool result = true;
            string remark = null;

            string payAccountId = null;
            if (accountName.IsNotNullOrEmpty())
            {
                var checkResult = await this.CheckOrAddPayAccount(jq, payType, accountName, accountConfig);
                remark = checkResult.Item2;
                payAccountId = checkResult.Item1;
                if (string.IsNullOrEmpty(payAccountId))
                {
                    result = false;
                    remark = $"账号不存在：{accountName}";
                }
            }

            if (result)
            {
                Dictionary<string, List<DDRuleList>> payTypeRuleDic = await this.GetRuleDic(jq, "payType");
                List<DDRuleList> existRuleList = new List<DDRuleList>(0);
                if (payTypeRuleDic.ContainsKey(payType))
                {
                    existRuleList.AddRange(payTypeRuleDic[payType]);
                }
                List<string> successedSiteList = new List<string>(siteCodeList.Count);
                List<string> replaceedSiteList = new List<string>(siteCodeList.Count);
                List<string> failedSiteList = new List<string>(siteCodeList.Count);
                DDRuleList existRule = null;
                foreach (string siteCode in siteCodeList)
                {
                    existRule = existRuleList.FirstOrDefault(m => m.Rule.SiteCode.Equals(siteCode, StringComparison.OrdinalIgnoreCase));
                    if (existRule != null)
                    {
                        await this.DelRuleDic(jq, existRule.Rule.Id);
                    }
                    var addRuleResult = await this._httpClient.PostJson($"https://dd.{jq}/adm/rule/adddo", JsonConvert.SerializeObject(new
                    {
                        isOpen = false,
                        //1=循环
                        limitType = "1",
                        paymentMethod = payType,
                        siteClusterCode = clusterCode,
                        siteCode = siteCode,
                        ruleItems = new List<object>
                        {
                            new {
                                //0=默认
                                combinationType="0",
                                //0=不限制金额
                                limitAmount="0",
                                //0=不限制笔数
                                limitTrans="0",
                                sort=1,
                                payAccountId=payAccountId,
                                payId=payId
                            }
                        }
                    }), new Dictionary<string, string>
                    {
                        {"Cookie", this.jqAuthDic[jq] }
                    });
                    if (addRuleResult.Item1 != System.Net.HttpStatusCode.OK)
                    {
                        result = false;
                        failedSiteList.Add(siteCode);
                    }
                    else
                    {
                        if (existRule == null)
                        {
                            successedSiteList.Add(siteCode);
                        }
                        else
                        {
                            replaceedSiteList.Add(siteCode);
                        }
                    }
                }

                remark = $"详情说明：成功{successedSiteList.Count}个：{string.Join(",", successedSiteList)}，替换{replaceedSiteList.Count}个：{string.Join(",", replaceedSiteList)}，失败{failedSiteList.Count}个：{string.Join(",", failedSiteList)}";
            }

            return (result, remark);
        }

        /// <summary>
        /// 添加笔数限制规则
        /// </summary>
        /// <param name="jq"></param>
        /// <param name="clusterCode"></param>
        /// <param name="payId"></param>
        /// <param name="payType"></param>
        /// <param name="siteCodeList"></param>
        /// <param name="accountName"></param>
        /// <param name="accountConfig"></param>
        /// <returns></returns>
        public async Task<(bool, string)> AddNumberTransactionsRuleDic(string jq, string clusterCode, string payId, string payType, string siteCode, List<string> accountIdList)
        {
            bool result = true;
            string remark = null;

            Dictionary<string, List<DDRuleList>> payTypeRuleDic = await this.GetRuleDic(jq, "payType");
            List<DDRuleList> existRuleList = new List<DDRuleList>(0);
            if (payTypeRuleDic.ContainsKey(payType))
            {
                existRuleList.AddRange(payTypeRuleDic[payType]);
            }
            List<string> successedSiteList = new List<string>();
            List<string> replaceedSiteList = new List<string>();
            List<string> failedSiteList = new List<string>();
            DDRuleList existRule = null;

            List<object> ruleItemList = new List<object>(accountIdList.Count);

            int sort = 0;
            foreach (string payAccountId in accountIdList)
            {
                sort++;
                ruleItemList.Add(new
                {
                    //1=限制仅收1笔
                    limitTrans = "1",
                    sort = sort,
                    payAccountId = payAccountId,
                    payId = payId
                });
            }

            existRule = existRuleList.FirstOrDefault(m => m.Rule.SiteCode.Equals(siteCode, StringComparison.OrdinalIgnoreCase));
            if (existRule != null)
            {
                await this.DelRuleDic(jq, existRule.Rule.Id);
            }
            var addRuleResult = await this._httpClient.PostJson($"https://dd.{jq}/adm/rule/adddo", JsonConvert.SerializeObject(new
            {
                isOpen = false,
                //3=笔数
                limitType = "3",
                //2=月
                agingType = "2",
                paymentMethod = payType,
                siteClusterCode = clusterCode,
                siteCode = siteCode,
                ruleItems = ruleItemList
            }), new Dictionary<string, string>
            {
                {"Cookie", this.jqAuthDic[jq] }
            });
            if (addRuleResult.Item1 != System.Net.HttpStatusCode.OK)
            {
                result = false;
                failedSiteList.Add(siteCode);
            }
            else
            {
                if (existRule == null)
                {
                    successedSiteList.Add(siteCode);
                }
                else
                {
                    replaceedSiteList.Add(siteCode);
                }
            }


            remark = $"详情说明：成功{successedSiteList.Count}个：{string.Join(",", successedSiteList)}，替换{replaceedSiteList.Count}个：{string.Join(",", replaceedSiteList)}，失败{failedSiteList.Count}个：{string.Join(",", failedSiteList)}";

            return (result, remark);
        }

        public async Task<List<PayOrderDetail>> GetPayOrderDetailList(string jq, string payAccountName, DateTime startDate, DateTime endDate)
        {
            var getPayResult = await this._httpClient.Get($"https://dd.{jq}/adm/PayAccount/GetPayDetailList?payAccountName={payAccountName}&startDate={startDate.ToString("yyyy-MM-dd")}&endDate={endDate.ToString("yyyy-MM-dd")}", new Dictionary<string, string>
            {
                {"Cookie", this.jqAuthDic[jq] }
            });
            List<PayOrderDetail> ddPayList = JArray.Parse(getPayResult.Item2).ToObject<List<PayOrderDetail>>();
            return ddPayList ?? new List<PayOrderDetail>(0);
        }
    }

    public class DDPay
    {
        public string Id { get; set; }
        public string Location { get; set; }
    }

    public class DDRuleList
    {
        public DDRule Rule { get; set; }
        public List<DDRuleItem> RuleItems { get; set; }
        public string RuleItemsShowContent { get; set; }
    }
    public class DDRule
    {
        public string Id { get; set; }
        public string SiteClusterCode { get; set; }
        public string PaymentMethod { get; set; }
        public string SiteCode { get; set; }
        public DateTime CreateDateTime { get; set; }
    }
    public class DDRuleItem
    {
        public string PayId { get; set; }
        public string PayHost { get; set; }
        public string PayAccountId { get; set; }
        public string PayAccountName { get; set; }
    }

    public class DDPayAccount
    {
        public string Id { get; set; }
        public string PaymentMethod { get; set; }
        public string Name { get; set; }
        public object AccountConfig { get; set; }
        public int Status { get; set; }
    }
    public class PayOrderDetail
    {
        public string Token { get; set; }
        public string PayAccountId { get; set; }
        public decimal PayAmount { get; set; }
        public DateTime CreateDateTime { get; set; }
    }
}
