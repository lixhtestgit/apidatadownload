using System;
using WebApplication1.DB.Extend;
using WebApplication1.Enum;

namespace WebApplication1.DB.CMS
{
    /// <summary>
    /// 统计_订单
    /// </summary>
    [ClassMapper(EDBConnectionType.SqlServer, "dbo", "TJ_TB_Order")]
    public class TJ_TB_Order
    {
        [PropertyMapper]
        public int ID{ get; set; }
        [PropertyMapper]
        public int OriginID { get; set; }
        [PropertyMapper]
        public int ProductCount { get;set;}
        [PropertyMapper]
        public DateTime AddTime{ get; set; }
        [PropertyMapper]
        public string Tx{ get; set; }
        [PropertyMapper]
        public decimal Price_Product{ get; set; }
        [PropertyMapper]
        public decimal Price_Count{ get; set; }
        [PropertyMapper]
        public decimal Price_PreCount{ get; set; }
        [PropertyMapper]
        public decimal Price_PreCount1{ get; set; }
        [PropertyMapper]
        public decimal Shipping{ get; set; }
        [PropertyMapper]
        public int State{ get; set; }
        [PropertyMapper]
        public int UserID{ get; set; }
        [PropertyMapper]
        public string UserEmail { get; set; }
        [PropertyMapper]
        public string IP { get; set; }
        [PropertyMapper]
        public int Address1 { get; set; }
        [PropertyMapper]
        public int Address2 { get; set; }
        [PropertyMapper]
        public int CountryID { get; set; }
        [PropertyMapper]
        public DateTime? PayTime{ get; set; }
        [PropertyMapper]
        public int PayType{ get; set; }
        [PropertyMapper]
        public int IsFastOrder{ get; set; }
        [PropertyMapper]
        public int FastID{ get; set; }
        [PropertyMapper]
        public int IsComment{ get; set; }
        [PropertyMapper]
        public int SiteID{ get; set; }
        [PropertyMapper]
        public int OriginSiteID { get; set; }
        [PropertyMapper]
        public int RemarkState{ get; set; }
        [PropertyMapper]
        public int? RefundType{ get; set; }
        [PropertyMapper]
        public int IsDisputed{ get; set; }
        [PropertyMapper]
        public int IsDanger{ get; set; }
        [PropertyMapper]
        public int Currency{ get; set; }
        [PropertyMapper]
        public string CurrencyName{ get; set; }
        [PropertyMapper]
        public decimal CurrencyRate{ get; set; }
        [PropertyMapper]
        public decimal CurrencyPrice{ get; set; }
        [PropertyMapper]
        public string PayCurrencyName{ get; set; }
        [PropertyMapper]
        public decimal PayCurrencyPrice{ get; set; }
        [PropertyMapper]
        public int IsFastPaypal{ get; set; }
        [PropertyMapper]
        public int IsGuest{ get; set; }
        [PropertyMapper]
        public decimal OriginalPrice{ get; set; }
        [PropertyMapper]
        public decimal CurrencyShipping { get; set; }
        [PropertyMapper]
        public decimal CurrencyShippingMonery{ get; set; }
        [PropertyMapper]
        public string tag{ get; set; }
    }
}
