using Newtonsoft.Json;
using System.Collections.Generic;

namespace WebApplication1
{
    public class MeShopCultureTran
    {
        /// <summary>
        /// 语言名称
        /// </summary>
        public string CultureName { get; set; }

        /// <summary>
        /// 页面语言翻译
        /// </summary>
        [JsonIgnore]
        public List<MeShopPageTran> MeShopPageTranList { get; set; }

        /// <summary>
        /// 显示的页面SQL语言翻译
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, Dictionary<string, string>> ShowSqlMeShopPageTranDic
        {
            get
            {
                Dictionary<string, Dictionary<string, string>> dic = new Dictionary<string, Dictionary<string, string>>(this.MeShopPageTranList.Count);
                foreach (var cultureItem in this.MeShopPageTranList)
                {
                    Dictionary<string, string> pageDic = new Dictionary<string, string>(cultureItem.PageKeyTranDic.Count);
                    foreach (var pageItem in cultureItem.PageKeyTranDic)
                    {
                        string value = pageItem.Value?.Replace("'", "''").Replace("\"", "\\\"") ?? "";
                        pageDic.Add(pageItem.Key, value);
                    }
                    dic.Add(cultureItem.PageName, pageDic);
                }
                return dic;
            }
        }

        /// <summary>
        /// 显示的页面JSON语言翻译
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> ShowJsonMeShopPageTranDic
        {
            get
            {
                Dictionary<string, Dictionary<string, string>> dic = new Dictionary<string, Dictionary<string, string>>(this.MeShopPageTranList.Count);
                foreach (var cultureItem in this.MeShopPageTranList)
                {
                    Dictionary<string, string> pageDic = new Dictionary<string, string>(cultureItem.PageKeyTranDic.Count);
                    foreach (var pageItem in cultureItem.PageKeyTranDic)
                    {
                        string value = pageItem.Value ?? "";
                        pageDic.Add(pageItem.Key, value);
                    }
                    dic.Add(cultureItem.PageName, pageDic);
                }
                return dic;
            }
        }

    }

    public class MeShopPageTran
    {
        /// <summary>
        /// 页面名称
        /// </summary>
        public string PageName { get; set; }

        /// <summary>
        /// 页面Key和多语言字典
        /// </summary>
        public Dictionary<string, string> PageKeyTranDic { get; set; }
    }

}
