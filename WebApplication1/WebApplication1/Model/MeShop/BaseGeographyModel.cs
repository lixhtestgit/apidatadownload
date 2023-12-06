using Newtonsoft.Json;

namespace WebApplication1.Model.MeShop
{
    /// <summary>
    /// 配置中心国家省州数据
    /// </summary>
    public class BaseGeographyModel
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [JsonProperty("id")]
        public long ID { get; set; }
        /// <summary>
        /// 国家名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
        /// <summary>
        /// 国家名称
        /// 多语言
        /// </summary>
        [JsonProperty("cultureName")]
        public string CultureName { get; set; }
        /// <summary>
        /// 国家简码
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }
        /// <summary>
        /// 国家三位简码
        /// </summary>
        [JsonProperty("code3")]
        public string Code3 { get; set; }
        /// <summary>
        /// 所在洲（亚洲/欧洲等）
        /// </summary>
        [JsonProperty("continent")]
        public string Continent { get; set; }
        /// <summary>
        /// 电话前缀
        /// </summary>
        [JsonProperty("phone_number_prefix")]
        public int PhoneNumberPrefix { get; set; }
        /// <summary>
        /// 自动填充字段
        /// </summary>
        [JsonProperty("autocompletion_field")]
        public string AutocompletionField { get; set; }
        /// <summary>
        /// 省州类型
        /// </summary>
        [JsonProperty("province_key")]
        public string ProvinceKey { get; set; }
        /// <summary>
        /// 枚举:1 国家  2 省州
        /// </summary>
        [JsonProperty("type")]
        public int Type { get; set; }
        /// <summary>
        /// 父级国家简码
        /// </summary>
        [JsonProperty("parent_code")]
        public string ParentCode { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        [JsonProperty("state")]
        public int State { get; set; }
    }
}
