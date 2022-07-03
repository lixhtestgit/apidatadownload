using PPPayReportTools.Excel;

namespace WebApplication1.Model.PayNotify
{
    public class PayCompanyOrder
    {
        /// <summary>
        /// 会话ID
        /// </summary>
        [ExcelTitle("Order_ID")]
        public string SessionID { get; set; }
    }
}
