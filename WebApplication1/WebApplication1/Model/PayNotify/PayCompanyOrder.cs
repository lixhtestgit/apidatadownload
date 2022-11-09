using PPPayReportTools.Excel;

namespace WebApplication1.Model.PayNotify
{
    public class PayCompanyOrder
    {
        /// <summary>
        /// 会话ID
        /// </summary>
        [ExcelTitle("SessionID")]
        public string SessionID { get; set; }

        /// <summary>
        /// Xendit订单标识
        /// </summary>
        [ExcelTitle("Invoice_ID")]
        public string XenditInvoiceID { get; set; }

        /// <summary>
        /// 交易号
        /// </summary>
        [ExcelTitle("TX")]
        public string TX { get; set; }
    }
}
