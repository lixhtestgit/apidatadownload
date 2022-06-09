using PPPayReportTools.Excel;

namespace WebApplication1.Model.PayNotify
{
    public class CashfreeOrder
    {
        /// <summary>
        /// 会话ID
        /// </summary>
        [ExcelTitle("Order Id")]
        public string SessionID { get; set; }
    }
}
