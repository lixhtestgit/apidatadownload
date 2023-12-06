using PPPayReportTools.Excel;

namespace WebApplication1.Model.MeShop
{
    public class MeshopExcelModel
    {
        /// <summary>
        /// 英语
        /// </summary>
        [ExcelTitle("英语")]
        public string En { get; set; }

        ///// <summary>
        ///// 德语
        ///// </summary>
        //[ExcelTitle("德语")]
        //public string De { get; set; }

        ///// <summary>
        ///// 法语
        ///// </summary>
        //[ExcelTitle("法语")]
        //public string Fr { get; set; }

        ///// <summary>
        ///// 日语
        ///// </summary>
        //[ExcelTitle("日语")]
        //public string Ja { get; set; }

        ///// <summary>
        ///// 意大利语
        ///// </summary>
        //[ExcelTitle("意大利语")]
        //public string It { get; set; }

        /// <summary>
        /// 葡萄牙语
        /// </summary>
        [ExcelTitle("葡语")]
        public string Pt { get; set; }

        /// <summary>
        /// 西班牙语
        /// </summary>
        [ExcelTitle("西班牙语")]
        public string Es { get; set; }
    }
}
