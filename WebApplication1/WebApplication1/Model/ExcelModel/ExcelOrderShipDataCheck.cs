using PPPayReportTools.Excel;
using WebApplication1.ExcelCsv;

namespace WebApplication1.Model.ExcelModel
{
    /// <summary>
    /// 订单发货数据检测
    /// </summary>
    public class ExcelOrderShipDataCheck
    {
        [ExcelTitle("ShopName")]
        [CsvColumn()]
        public string ShopName { get; set; }

        [ExcelTitle("OrderID")]
        [CsvColumn()]
        public string OrderID { get; set; }

        [ExcelTitle("ChoiseCurrency")]
        [CsvColumn()]
        public string ChoiseCurrency { get; set; }

        [ExcelTitle("ChoiseCurrencyRate")]
        public decimal ChoiseCurrencyRate { get; set; }

        [ExcelTitle("CurrencyTotalPayPrice")]
        [CsvColumn()]
        public decimal CurrencyTotalPayPrice { get; set; }

        [ExcelTitle("TotalUSDPayPrice")]
        [CsvColumn()]
        public decimal TotalUSDPayPrice { get; set; }

        [ExcelTitle("OrderShipState")]
        [CsvColumn()]
        public string OrderShipState { get; set; }

        [ExcelTitle("OrderShipName")]
        [CsvColumn()]
        public string OrderShipName { get; set; }

        [ExcelTitle("PayTime")]
        [CsvColumn()]
        public string PayTime { get; set; }

        [ExcelTitle("FirstName")]
        [CsvColumn()]
        public string FirstName { get; set; }

        [ExcelTitle("LastName")]
        [CsvColumn()]
        public string LastName { get; set; }

        [ExcelTitle("CountryCode")]
        [CsvColumn()]
        public string CountryCode { get; set; }

        [ExcelTitle("ProvinceCode")]
        [CsvColumn()]
        public string ProvinceCode { get; set; }

        [ExcelTitle("Province")]
        [CsvColumn()]
        public string Province { get; set; }

        [ExcelTitle("City")]
        [CsvColumn()]
        public string City { get; set; }

        [ExcelTitle("Address1", true)]
        [CsvColumn()]
        public string Address1 { get; set; }

        [ExcelTitle("ZIP")]
        [CsvColumn()]
        public string ZIP { get; set; }

        [ExcelTitle("Phone")]
        [CsvColumn()]
        public string Phone { get; set; }

        [ExcelTitle("OrderItemID")]
        [CsvColumn()]
        public string OrderItemID { get; set; }

        [ExcelTitle("FreightName")]
        [CsvColumn()]
        public string FreightName { get; set; }

        [ExcelTitle("ShipNumber")]
        [CsvColumn()]
        public string ShipNumber { get; set; }

        [ExcelTitle("ShipGroup")]
        [CsvColumn()]
        public string ShipGroup { get; set; }

        [ExcelTitle("ShipUrl")]
        [CsvColumn()]
        public string ShipUrl { get; set; }

        [ExcelTitle("ShipState")]
        [CsvColumn()]
        public string ShipState { get; set; }

        [ExcelTitle("ShipTime")]
        [CsvColumn()]
        public string ShipTime { get; set; }

        [ExcelTitle("Tag")]
        [CsvColumn()]
        public string Tag { get; set; }

        [ExcelTitle("Remark")]
        [CsvColumn()]
        public string Remark { get; set; }
    }
}
