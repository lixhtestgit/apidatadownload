using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.Model;

namespace WebApplication1.BIZ
{
    /// <summary>
    /// 店铺权限业务类
    /// </summary>
    public class AuthBIZ
    {
        protected HttpClient PayHttpClient { get; set; }
        public AuthBIZ(
            IHttpClientFactory httpClientFactory
        )
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// 获取店铺权限
        /// </summary>
        /// <param name="shopUrl">aaa.meshopstore.com</param>
        /// <param name="email">登录邮箱</param>
        /// <param name="password">登录密码</param>
        /// <returns></returns>
        public async Task<Dictionary<string, string>> GetShopAuthDic(string shopUrl, string email, string password)
        {
            string shopName = shopUrl.Split('.')[0];
            string shopRootDomain = shopUrl.Remove(0, shopUrl.IndexOf('.') + 1);
            string ssoLoginUrl = $"https://sso.{shopRootDomain}/Auth/Login";
            Dictionary<string, string> formDic = new Dictionary<string, string>
            {
                {"email",email },
                {"password",password },
                {"shopUrl", shopName}
            };
            var authResult = await this.PayHttpClient.PostForm(ssoLoginUrl, formDic, null);

            ShopAuth shopAuth = JsonConvert.DeserializeObject<ShopAuth>(authResult.Item2);

            return new Dictionary<string, string>
            {
                {"Authorization",$"Bearer {shopAuth.access_token}" },
                {"access_hash",shopAuth.access_hash },
                {"response_in",shopAuth.response_in }
            };
        }
    }
}
