using PPPayReportTools.Excel;

namespace WebApplication1.Model.ExcelModel
{
    /// <summary>
    /// 运单号上传文件信息
    /// </summary>
    public class ExcelOrderShipFile
    {
        [ExcelCell(cellCoordinateExpress: "A1")]
        public string ShopUrl { get; set; }
    }
}
