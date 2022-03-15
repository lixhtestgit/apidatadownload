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

        [ExcelTitle("支付方式")]
        public string PayType { get; set; }

        [ExcelTitle("相关日志")]
        public string Content { get; set; }
    }
}
