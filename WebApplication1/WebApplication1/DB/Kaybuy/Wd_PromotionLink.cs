using System;
using WebApplication1.DB.Extend;
using WebApplication1.Enum;

namespace WebApplication1.DB.CMS
{
    [ClassMapper(EDBSiteName.Kaybuy, "dbo", "Wd_PromotionLink")]
    public class Wd_PromotionLink
    {
        /// <summary>
        /// 主键
        /// </summary>
        [PropertyMapper(isPrimaryKey: true, isIdentityInsert: true)]
        public int Wt_ID { get; set; }
        /// <summary>
        /// 产品原始链接
        /// </summary>
        [PropertyMapper]
        public string Wt_DownloadUrl { get; set; }
        /// <summary>
        /// 产品标识
        /// </summary>
        [PropertyMapper]
        public string Wt_SpuID { get; set; }
        /// <summary>
        /// 产品标题
        /// </summary>
        [PropertyMapper]
        public string Wt_Title { get; set; }
        /// <summary>
        /// 折扣类型(0=折扣,1=抵扣,2=固定金额)
        /// </summary>
        [PropertyMapper]
        public int Wt_DiscountType { get; set; }
        /// <summary>
        /// 折扣值
        /// </summary>
        [PropertyMapper]
        public decimal Wt_DiscountValue { get; set; }
        /// <summary>
        /// 产品链接
        /// </summary>
        [PropertyMapper]
        public string Wt_Url { get; set; }
        /// <summary>
        /// 开始时间
        /// </summary>
        [PropertyMapper]
        public DateTime Wt_BeginTime { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        [PropertyMapper]
        public DateTime Wt_EndTime { get; set; }
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
    }
}
