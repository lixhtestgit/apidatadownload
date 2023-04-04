using PPPayReportTools.Excel;
using System.Collections.Generic;
using System.Linq;
using WebApplication1.ExcelCsv;

namespace WebApplication1.Model.ExcelModel
{
    public class ExcelProductSpu
    {
        /// <summary>
        /// 产品地址
        /// </summary>
        [ExcelTitle("Handle", true)]
        [CsvColumn("Handle", true)]
        public string Handle { get; set; }

        /// <summary>
        /// 产品ID
        /// </summary>
        [ExcelTitle("SpuID")]
        public string SpuID { get; set; }

        /// <summary>
        /// 新产品集合
        /// </summary>
        [ExcelTitle("NewCollIDS")]
        public string NewCollIDS
        {
            get
            {
                return string.Join(",", this.NewCollIDList);
            }
            set
            {
                this.NewCollIDList = value.Split(',').ToList();
            }
        }

        public List<string> NewCollIDList { get; set; }
    }
}
