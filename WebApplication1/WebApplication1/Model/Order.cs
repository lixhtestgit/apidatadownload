using PPPayReportTools.Excel;
using System;

namespace WebApplication1.Model
{
    public class Order
    {
        [ExcelTitle("弃单号")]
        public string OrderGuid { get; set; }

        [ExcelTitle("创建时间")]
        public DateTime CreateTime { get; set; }

        [ExcelTitle("客户")]
        public string UserName { get; set; }

        [ExcelTitle("电子邮件状态")]
        public string IsSended { get; set; }

        [ExcelTitle("恢复状态")]
        public string State { get; set; }

        [ExcelTitle("总额")]
        public string TotalPrice { get; set; }
    }
}
