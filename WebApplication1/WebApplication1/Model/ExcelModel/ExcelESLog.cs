using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication1.Extension;

namespace WebApplication1.Model.ExcelModel
{
    /// <summary>
    /// Excel--ESLog
    /// </summary>
    public class ExcelESLog
    {
        [ExcelTitle("SessionID")]
        public string SessionID { get; set; }

        //[ExcelTitle("Log")]
        //public string Log { get; set; }

        //[ExcelTitle("CartNo")]
        //public string CartNo { get; set; }

        public List<DateTime> LogTimeList { get; set; }

        [ExcelTitle("LogTimeListStr")]
        public string LogTimeListStr
        {
            get
            {
                string log = string.Join('\n', this.LogTimeList.Select(m => m.ToString_yyyyMMddHHmmss()));
                return log;
            }
        }
    }
}
