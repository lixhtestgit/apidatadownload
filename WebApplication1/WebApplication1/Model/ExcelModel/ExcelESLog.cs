using PPPayReportTools.Excel;

namespace WebApplication1.Model.ExcelModel
{
    /// <summary>
    /// Excel--ESLog
    /// </summary>
    public class ExcelESLog
    {
        [ExcelTitle("SessionID")]
        public string SessionID { get; set; }

        [ExcelTitle("Log")]
        public string Log { get; set; }

        [ExcelTitle("CartNo")]
        public string CartNo { get; set; }
    }
}
