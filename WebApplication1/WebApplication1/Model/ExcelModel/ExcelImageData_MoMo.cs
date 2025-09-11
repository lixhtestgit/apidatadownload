using PPPayReportTools.Excel;
using System;

namespace WebApplication1.Model.ExcelModel
{
    /// <summary>
    /// 图片数据-MoMo
    /// </summary>
    public class ExcelImageData_MoMo
    {
        [ExcelTitle("序号")]
        public string Index { get; set; }

        [ExcelTitle("采集链接")]
        public string ProductUrl { get; set; }

        [ExcelTitle("店铺")]
        public string ShopHost { get; set; }

        [ExcelTitle("备注")]
        public string Remark { get; set; }

        [ExcelTitle("XPath")]
        public string XPath { get; set; }
    }
}
