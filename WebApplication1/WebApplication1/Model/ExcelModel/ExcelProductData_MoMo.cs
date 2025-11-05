using PPPayReportTools.Excel;

namespace WebApplication1.Model.ExcelModel
{
    /// <summary>
    /// 产品数据-MoMo
    /// </summary>
    public class ExcelProductData_MoMo
    {
        [ExcelTitle("采集链接")]
        public string Wt_ProductUrl { get; set; }

        [ExcelTitle("人民币¥")]
        public string Wt_ProductPrice { get; set; }
    }
}
