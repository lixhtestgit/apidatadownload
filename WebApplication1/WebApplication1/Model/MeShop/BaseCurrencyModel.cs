using Newtonsoft.Json;
using System;

namespace WebApplication1.Model.MeShop
{
    public class BaseCurrencyModel
    {
        /// <summary>
        /// ID
        /// </summary>
        [JsonProperty("id")]
        public long ID { get; set; }
        /// <summary>
        /// 币种名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
        /// <summary>
        /// 币种名称+符号
        /// </summary>
        [JsonProperty("symbol")]
        public string Symbol { get; set; }
        /// <summary>
        /// 币种符号
        /// </summary>
        [JsonProperty("short_symbol")]
        public string ShortSymbol { get; set; }
        /// <summary>
        /// 币种汇率
        /// </summary>
        [JsonProperty("rate")]
        public decimal Rate { get; set; }
        /// <summary>
        /// 保留位数
        /// </summary>
        [JsonProperty("digit")]
        public int Digit { get; set; }
        /// <summary>
        /// 排序
        /// </summary>
        [JsonProperty("sort")]
        public int Sort { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        [JsonProperty("state")]
        public int State { get; set; }
        /// <summary>
        /// 创建人
        /// </summary>
        [JsonProperty("create_user_name")]
        public string CreateUserName { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonProperty("create_time")]
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 修改人
        /// </summary>
        [JsonProperty("update_user_name")]
        public string UpdateUserName { get; set; }
        /// <summary>
        /// 修改时间
        /// </summary>
        [JsonProperty("update_time")]
        public DateTime UpdateTime { get; set; }
    }
}
