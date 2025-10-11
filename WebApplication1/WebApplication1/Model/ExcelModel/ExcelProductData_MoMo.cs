using PPPayReportTools.Excel;

namespace WebApplication1.Model.ExcelModel
{
    /// <summary>
    /// 产品数据-MoMo
    /// </summary>
    public class ExcelProductData_MoMo
    {
        [ExcelTitle("商品标题")]
        public string Wt_Title { get; set; }

        [ExcelTitle("商品图片URL")]
        public string Wt_Image { get; set; }

        [ExcelTitle("商品价格")]
        public decimal Wt_Price { get; set; }

        [ExcelTitle("来源商城")]
        public string Wt_OriginProductMall { get; set; }

        [ExcelTitle("来源商品ID")]
        public string Wt_OriginProductID { get; set; }
    }
}
