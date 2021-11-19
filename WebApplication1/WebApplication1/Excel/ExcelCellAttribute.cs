using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPPayReportTools.Excel
{
    /// <summary>
    /// Excel单元格标记特性
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false)]
    public class ExcelCellAttribute : System.Attribute
    {
        /// <summary>
        /// 该参数用于收集数据存于固定位置的单元格数据（单元格坐标表达式（如：A1,B2,C1+C2...横坐标使用26进制字母，纵坐标使用十进制数字））
        /// </summary>
        public string CellCoordinateExpress { get; set; }

        /// <summary>
        /// 该参数用于替换模板文件的预定义变量使用（{A} {B}）
        /// </summary>
        public string CellParamName { get; set; }

        /// <summary>
        /// 字符输出格式（数字和日期类型需要）
        /// </summary>
        public string OutputFormat { get; set; }

        public ExcelCellAttribute(string cellCoordinateExpress = null, string cellParamName = null)
        {
            CellCoordinateExpress = cellCoordinateExpress;
            CellParamName = cellParamName;
        }

        public ExcelCellAttribute(string cellCoordinateExpress, string cellParamName, string outputFormat) : this(cellCoordinateExpress, cellParamName)
        {
            OutputFormat = outputFormat;
        }
    }
}
