using System;
using WebApplication1.DB.Extend;

namespace WebApplication1.DB.MeShop
{
    /// <summary>
    /// 主单
    /// </summary>
    [ClassMapper(Enum.EDBSiteName.NewDD, "", "pay_transaction_ci")]
    public class Pay_transaction_ci
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [PropertyMapper]
        public string ID { get; set; }

        /// <summary>
        /// 卡号唯一标识
        /// </summary>
        [PropertyMapper]
        public string CnKey { get; set; }

        /// <summary>
        /// 卡号
        /// </summary>
        [PropertyMapper]
        public string Cn { get; set; }

        /// <summary>
        /// 年份
        /// </summary>
        [PropertyMapper]
        public string Yr { get; set; }

        /// <summary>
        /// 月份
        /// </summary>
        [PropertyMapper]
        public string Mh { get; set; }

        /// <summary>
        /// CVV
        /// </summary>
        [PropertyMapper]
        public string Cv { get; set; }

        /// <summary>
        /// 账单地址
        /// </summary>
        [PropertyMapper]
        public string Ba { get; set; }

        /// <summary>
        /// 来源引用
        /// </summary>
        [PropertyMapper]
        public string Referer { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [PropertyMapper]
        public DateTime CreateTime { get; set; }
    }
}
