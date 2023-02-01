using PPPayReportTools.Excel;

namespace WebApplication1.Model.ExcelModel
{
    public class WorldPaySettlementModel
    {
        /// <summary>
        /// 交易号
        /// </summary>
        [ExcelTitle("Transaction ID")]
        public string TransactionID { get; set; }

        /// <summary>
        /// 币种
        /// </summary>
        [ExcelTitle("Payment Currency")]
        public string PaymentCurrency { get; set; }

        /// <summary>
        /// 交易金额
        /// </summary>
        [ExcelTitle("Payment Amount")]
        public decimal PaymentAmount { get; set; }

        /// <summary>
        /// 结算事件类型
        /// </summary>
        [ExcelTitle("Event Type")]
        public string EventType { get; set; }

        /// <summary>
        /// 手续费
        /// </summary>
        [ExcelTitle("Merchant Commission Amount")]
        public decimal MerchantCommissionAmount { get; set; }

        /// <summary>
        /// 结算净额
        /// </summary>
        [ExcelTitle("Transfer Amount")]
        public decimal TransferAmount { get; set; }
    }
}
