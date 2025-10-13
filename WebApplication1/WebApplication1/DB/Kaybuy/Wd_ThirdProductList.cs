using System;
using WebApplication1.DB.Extend;
using WebApplication1.Enum;

namespace WebApplication1.DB.CMS
{
    /// <summary>
    /// 三方产品列表
    /// </summary>
    [ClassMapper(EDBSiteName.Kaybuy, "dbo", "Wd_ThirdProductList")]
    public class Wd_ThirdProductList
    {
        /// <summary>
        /// 主键
        /// </summary>
        [PropertyMapper(isPrimaryKey: true, isIdentityInsert: true)]
        public int Wt_ID { get; set; }
        /// <summary>
        /// 是否已删除
        /// </summary>
        [PropertyMapper]
        public int Wt_IsDelete { get; set; }
        /// <summary>
        /// GUID唯一标识
        /// </summary>
        [PropertyMapper]
        public string Wt_CurrentGuID { get; set; }
        /// <summary>
        /// 是否启用
        /// </summary>
        [PropertyMapper]
        public int? Wt_IsTrue { get; set; }
        /// <summary>
        /// 添加时间
        /// </summary>
        [PropertyMapper]
        public DateTime? Wt_AddTime { get; set; }
        /// <summary>
        /// 商品标题
        /// </summary>
        [PropertyMapper]
        public string Wt_Title { get; set; }
        /// <summary>
        /// 商品图片URL
        /// </summary>
        [PropertyMapper]
        public string Wt_Image { get; set; }
        /// <summary>
        /// 商品价格
        /// </summary>
        [PropertyMapper]
        public decimal Wt_Price { get; set; }
        /// <summary>
        /// 来源商城（如：taobao,weidian,1688）
        /// </summary>
        [PropertyMapper]
        public string Wt_OriginProductMall { get; set; }
        /// <summary>
        /// 来源商品ID
        /// </summary>
        [PropertyMapper]
        public string Wt_OriginProductID { get; set; }
        /// <summary>
        /// 来源商品分类名称
        /// </summary>
        [PropertyMapper]
        public string Wt_OriginCollName { get; set; }
    }
}
