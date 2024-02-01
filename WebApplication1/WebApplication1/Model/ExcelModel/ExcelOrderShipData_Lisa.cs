using PPPayReportTools.Excel;
using System;

namespace WebApplication1.Model.ExcelModel
{
    /// <summary>
    /// 订单发货数据_Lisa
    /// </summary>
    public class ExcelOrderShipData_Lisa
    {
        [ExcelTitle("订单号")]
        public string OrderID { get; set; }

        [ExcelTitle("购买时间")]
        public DateTime PayTime { get; set; }

        [ExcelTitle("币种")]
        public string CountryCode { get; set; }

        [ExcelTitle("交易金额")]
        public decimal TotalPayPrice { get; set; }

        [ExcelTitle("购买方式")]
        public string PayMethod { get; set; }

        [ExcelTitle("商品名称")]
        public string ProductName { get; set; }

        [ExcelTitle("购买数量", true)]
        public string BuyCount { get; set; }

        [ExcelTitle("单价")]
        public string SKUPrice { get; set; }

        [ExcelTitle("订单描述")]
        public string OrderDescription { get; set; }

        [ExcelTitle("产品链接")]
        public string ProductLink { get; set; }

        [ExcelTitle("物流公司")]
        public string FreightName { get; set; }

        [ExcelTitle("运单号")]
        public string ShipNo { get; set; }
    }
}
