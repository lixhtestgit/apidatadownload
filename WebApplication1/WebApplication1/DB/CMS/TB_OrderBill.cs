using System;
using WebApplication1.DB.Extend;
using WebApplication1.Enum;

namespace WebApplication1.DB.CMS{    [ClassMapper(EDBConnectionType.SqlServer, "dbo", "TB_OrderBill")]    public class TB_OrderBill    {        [PropertyMapper]        public int ID{ get; set; }        [PropertyMapper]        public int OrderID { get; set; }        [PropertyMapper]        public int SiteID { get; set; }        [PropertyMapper]        public int ProductID { get; set; }        [PropertyMapper]        public int BuyCount { get; set; }
        [PropertyMapper]        public decimal Price { get; set; }        [PropertyMapper]        public string Remark { get; set; }    }}