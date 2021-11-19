using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PPPayReportTools.Excel
{
    /// <summary>
    /// 单元格字段映射类
    /// </summary>
    internal class ExcelCellFieldMapper
    {
        /// <summary>
        /// 属性信息
        /// </summary>
        public PropertyInfo PropertyInfo { get; set; }

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

        /// <summary>
        /// 获取对应关系_T属性添加了单元格映射关系
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<ExcelCellFieldMapper> GetModelFieldMapper<T>()
        {
            List<ExcelCellFieldMapper> fieldMapperList = new List<ExcelCellFieldMapper>(100);

            List<PropertyInfo> tPropertyInfoList = typeof(T).GetProperties().ToList();
            ExcelCellAttribute cellExpress = null;

            foreach (var item in tPropertyInfoList)
            {
                cellExpress = item.GetCustomAttribute<ExcelCellAttribute>();
                if (cellExpress != null)
                {
                    fieldMapperList.Add(new ExcelCellFieldMapper
                    {
                        CellCoordinateExpress = cellExpress.CellCoordinateExpress,
                        CellParamName = cellExpress.CellParamName,
                        OutputFormat = cellExpress.OutputFormat,
                        PropertyInfo = item
                    });
                }
            }

            return fieldMapperList;
        }
    }
}
