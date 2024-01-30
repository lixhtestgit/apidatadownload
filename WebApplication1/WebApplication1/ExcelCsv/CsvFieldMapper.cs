using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WebApplication1.ExcelCsv
{
    /// <summary>
    /// 字段映射类
    /// </summary>
    public class CsvFieldMapper
    {
        /// <summary>
        /// 属性信息
        /// </summary>
        public PropertyInfo PropertyInfo { get; set; }
        /// <summary>
        /// 标题
        /// </summary>
        public string CSVTitle { get; set; }
        /// <summary>
        /// 标题下标位置
        /// </summary>
        public int CSVTitleIndex { get; set; }
        /// <summary>
        /// 字符输出格式（数字和日期类型需要）
        /// </summary>
        public string OutputFormat { get; set; }
        /// <summary>
        /// 单元格是否要检查空数据（true为检查，为空的行数据不添加）
        /// </summary>
        public bool IsCheckContentEmpty { get; set; }

        public static List<CsvFieldMapper> GetModelFieldMapper<T>()
        {
            List<CsvFieldMapper> fieldMapperList = new List<CsvFieldMapper>(100);

            List<PropertyInfo> tPropertyInfoList = typeof(T).GetProperties().ToList();
            CsvColumnAttribute csvColumnAttribute = null;
            int beginTitleIndex = 0;
            foreach (var tPropertyInfo in tPropertyInfoList)
            {
                csvColumnAttribute = (CsvColumnAttribute)tPropertyInfo.GetCustomAttribute(typeof(CsvColumnAttribute));
                if (csvColumnAttribute != null)
                {
                    fieldMapperList.Add(new CsvFieldMapper
                    {
                        PropertyInfo = tPropertyInfo,
                        CSVTitle = csvColumnAttribute.Title,
                        CSVTitleIndex = beginTitleIndex,
                        OutputFormat = csvColumnAttribute.OutputFormat,
                        IsCheckContentEmpty = csvColumnAttribute.IsCheckContentEmpty
                    });
                    beginTitleIndex++;
                }
            }
            return fieldMapperList;
        }

    }
}
