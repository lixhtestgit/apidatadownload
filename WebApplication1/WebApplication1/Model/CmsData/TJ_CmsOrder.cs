using PPPayReportTools.Excel;
using System;

namespace WebApplication1.Model.CmsData
{
    /// <summary>
    /// 统计_CMS订单
    /// </summary>
    public class TJ_CmsOrder
    {
        //[ExcelTitle("原始站点ID")]
        //public int OriginSiteID { get; set; }
        //[ExcelTitle("原始站点名称")]
        //public string OriginSiteName { get; set; }
        //[ExcelTitle("原始订单ID")]
        //public string OriginOrderID { get; set; }
        //[ExcelTitle("站点ID")]
        //public int SiteID { get; set; }
        [ExcelTitle("站点名称")]
        public string SiteName { get; set; }
        [ExcelTitle("订单号")]
        public string OrderID { get; set; }
        [ExcelTitle("创建时间")]
        public DateTime AddTime { get; set; }
        [ExcelTitle("币种名称")]
        public string CurrencyName { get; set; }
        [ExcelTitle("多币种金额")]
        public double CurrencyPrice { get; set; }
        [ExcelTitle("美金金额")]
        public double USDPrice { get; set; }
        [ExcelTitle("备注")]
        public string Remark { get; set; }
    }
}
