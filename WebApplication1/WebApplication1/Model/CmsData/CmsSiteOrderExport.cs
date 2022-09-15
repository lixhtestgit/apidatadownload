using PPPayReportTools.Excel;
using System;

namespace WebApplication1.Model
{
    public class CmsSiteOrderExport
    {
        [ExcelTitle("站点")]
        public string SiteName { get; set; }

        [ExcelTitle("订单")]
        public string OrderID { get; set; }

        [ExcelTitle("子单号")]
        public string OrderBillID { get; set; }

        [ExcelTitle("订单金额")]
        public string Price_PreCount1 { get; set; }

        [ExcelTitle("品类")]
        public string CategoryName { get; set; }

        [ExcelTitle("SPUID")]
        public string SPUID { get; set; }

        [ExcelTitle("SKUID")]
        public string SKUID { get; set; }

        [ExcelTitle("付款时间")]
        public DateTime PayTime { get; set; }

        [ExcelTitle("付款方式")]
        public string PayTypeName { get; set; }

        [ExcelTitle("用户名")]
        public string UserName { get; set; }

        [ExcelTitle("注册时间")]
        public double RegDate { get; set; }

        [ExcelTitle("邮箱")]
        public string Email { get; set; }

        [ExcelTitle("地址1")]
        public string AddressLine1 { get; set; }

        [ExcelTitle("地址2")]
        public string AddressLine2 { get; set; }

        [ExcelTitle("电话")]
        public string Phone { get; set; }

        [ExcelTitle("国家")]
        public string CountryName { get; set; }

        [ExcelTitle("退款金额")]
        public string RefundPrice { get; set; }

        [ExcelTitle("退款原因")]
        public string RefundReson { get; set; }

        [ExcelTitle("订单留言")]
        public string OrderRemark { get; set; }
    }
}
