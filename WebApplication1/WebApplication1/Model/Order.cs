using PPPayReportTools.Excel;
using System;

namespace WebApplication1.Model
{
    public class Order
    {
        [ExcelTitle("弃单号")]
        public string OrderGuid { get; set; }

        [ExcelTitle("创建时间", outputFormat: "yyyy-MM-dd HH:mm:ss")]
        public DateTime CreateTime { get; set; }

        [ExcelTitle("恢复状态")]
        public string OrderState { get; set; }

        [ExcelTitle("支付方式")]
        public string ESPayType { get; set; }

        [ExcelTitle("SessionID")]
        public string SessionIDArrayStr { get; set; }

        [ExcelTitle("创建订单结果日志")]
        public string ESCreateOrderResultLog { get; set; }

        [ExcelTitle("支付结果日志")]
        public string ESPayResultLog { get; set; }
    }
}
