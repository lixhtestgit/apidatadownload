using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPPayReportTools.Excel
{
    public class ExcelFileDescription
    {
        /// <summary>
        /// 默认从第1行数据开始读取标题数据
        /// </summary>
        public ExcelFileDescription() : this(0)
        {
        }

        public ExcelFileDescription(int titleRowIndex)
        {
            this.TitleRowIndex = titleRowIndex;
        }

        /// <summary>
        /// 标题所在行位置（默认为0，没有标题填-1）
        /// </summary>
        public int TitleRowIndex { get; set; }

    }
}
