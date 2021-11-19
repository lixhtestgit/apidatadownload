using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPPayReportTools.Excel
{
    /// <summary>
    /// Excel标题标记特性
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false)]
    public class ExcelTitleAttribute : System.Attribute
    {
        /// <summary>
        /// Excel行标题(标题和下标选择一个即可)
        /// </summary>
        public string RowTitle { get; set; }
        /// <summary>
        /// Excel行下标(标题和下标选择一个即可,默认值-1)
        /// </summary>
        public int RowTitleIndex { get; set; }

        /// <summary>
        /// 单元格是否要检查空数据（true为检查，为空的行数据不添加）
        /// </summary>
        public bool IsCheckContentEmpty { get; set; }

        /// <summary>
        /// 字符输出格式（数字和日期类型需要）
        /// </summary>
        public string OutputFormat { get; set; }

        /// <summary>
        /// 是否是公式列
        /// </summary>
        public bool IsCoordinateExpress { get; set; }

        /// <summary>
        /// 标题特性构造方法
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="isCheckEmpty">单元格是否要检查空数据</param>
        /// <param name="isCoordinateExpress">是否是公式列</param>
        /// <param name="outputFormat">是否有格式化输出要求</param>
        public ExcelTitleAttribute(string title, bool isCheckEmpty = false, bool isCoordinateExpress = false, string outputFormat = "")
        {
            RowTitle = title;
            IsCheckContentEmpty = isCheckEmpty;
            IsCoordinateExpress = isCoordinateExpress;
            OutputFormat = outputFormat;
            RowTitleIndex = -1;
        }

        public ExcelTitleAttribute(int titleIndex, bool isCheckEmpty = false, bool isCoordinateExpress = false, string outputFormat = "")
        {
            RowTitleIndex = titleIndex;
            IsCheckContentEmpty = isCheckEmpty;
            IsCoordinateExpress = isCoordinateExpress;
            OutputFormat = outputFormat;
        }
    }
}
