using PPPayReportTools.Excel;

namespace WebApplication1.Model.ExcelModel
{
    /// <summary>
    /// 三方配置产品数据-MoMo
    /// </summary>
    public class ExcelThirdProductData_MoMo
    {
        [ExcelTitle("专题")]
        public string Wt_Coll { get; set; }

        [ExcelTitle("采集链接")]
        public string Wt_ProductUrl { get; set; }

        [ExcelTitle("标题")]
        public string Wt_ProductTitle { get; set; }

        [ExcelTitle("首图")]
        public string Wt_ProductImage { get; set; }

        [ExcelTitle("人民币¥")]
        public string Wt_ProductPrice { get; set; }

        [ExcelTitle("原始数据")]
        public string Wt_OriginProductDataJson { get; set; }
    }
}
