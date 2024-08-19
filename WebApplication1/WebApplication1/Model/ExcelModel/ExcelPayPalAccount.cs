using PPPayReportTools.Excel;

namespace WebApplication1.Model.ExcelModel
{
    public class ExcelPayPalAccount
    {
        [ExcelTitle("邮箱")]
        public string Email { get; set; }

        [ExcelTitle("ClientID")]
        public string ClientID { get; set; }

        [ExcelTitle("ClientSecret")]
        public string ClientSecret { get; set; }
    }
}
