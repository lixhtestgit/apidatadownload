using PPPayReportTools.Excel;

namespace WebApplication1.Model.ExcelModel
{
    /// <summary>
    /// Excel订单发货记录
    /// </summary>
    public class ExcelOrderShip
    {
        [ExcelTitle("订单号")]
        public long OrderID { get; set; }

        [ExcelTitle("子单号")]
        public long OrderItemID { get; set; }

        [ExcelTitle("子单产品数量")]
        public int OrderItemProductCount { get; set; }

        [ExcelTitle("承运商")]
        public string FreightName { get; set; }

        [ExcelTitle("运单号")]
        public string ShipNo { get; set; }

        [ExcelTitle("查询网址")]
        public string ShipNoSearchWebsite { get; set; }
    }
}
