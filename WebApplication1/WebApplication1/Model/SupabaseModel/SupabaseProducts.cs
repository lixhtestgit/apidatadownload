using Newtonsoft.Json.Linq;
using Supabase.Postgrest.Attributes;
using System;

namespace WebApplication1.Model.SupabaseModel
{
    [Table("products")]
    public class SupabaseProducts: SupabaseModelBase
    {
        /// <summary>
        /// 产品标题
        /// </summary>
        [Column("title")]
        public string Title { get; set; }

        /// <summary>
        /// 售价
        /// </summary>
        [Column("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// 原始价格
        /// </summary>
        [Column("original_price")]
        public decimal? OriginalPrice { get; set; }

        /// <summary>
        /// 品类
        /// </summary>
        [Column("category")]
        public string[] Category { get; set; }

        /// <summary>
        /// 产品描述
        /// </summary>
        [Column("description")]
        public string Description { get; set; }

        /// <summary>
        /// 品牌
        /// </summary>
        [Column("brand")]
        public string Brand { get; set; }

        /// <summary>
        /// 主图
        /// </summary>
        [Column("image")]
        public string Image { get; set; }

        /// <summary>
        /// 产品链接
        /// </summary>
        [Column("link")]
        public string Link { get; set; }

        /// <summary>
        /// 产品Json数据
        /// </summary>
        [Column("data")]
        public JObject Data { get; set; }

        /// <summary>
        /// 是否新品
        /// </summary>
        [Column("is_new")]
        public bool? IsNew { get; set; }

        /// <summary>
        /// 是否在售
        /// </summary>
        [Column("is_sale")]
        public bool? IsSale { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("created_at")]
        public DateTime CreateTime { get; set; }
    }
}
