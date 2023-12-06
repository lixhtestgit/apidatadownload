using PPPayReportTools.Excel;
using System;

namespace WebApplication1.Model.ExcelModel
{
    /// <summary>
    /// WP交易模型
    /// </summary>
    public class WorldPayTranscationModel
    {
        /// <summary>
        /// 商家号
        /// </summary>
        [ExcelTitle("Merchant Code")]
        public string MerchantCode { get; set; }

        /// <summary>
        /// 交易号
        /// </summary>
        [ExcelTitle("OrderCode")]
        public string OrderCode { get; set; }

        /// <summary>
        /// 时间
        /// </summary>
        [ExcelTitle("Date")]
        public DateTime Date { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [ExcelTitle("Status")]
        public string Status { get; set; }

        /// <summary>
        /// 币种
        /// </summary>
        [ExcelTitle("CurrencyCode")]
        public string CurrencyCode { get; set; }

        /// <summary>
        /// 金额
        /// </summary>
        [ExcelTitle("Amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 网站
        /// </summary>
        [ExcelTitle("Website")]
        public string Website { get; set; }
    }

    /// <summary>
    /// WP网站交易模型
    /// </summary>
    public class WorldPayWebsiteTranscationModel
    {
        [ExcelTitle("交易流水号")]
        public string Tx { get; set; }
    }
}
