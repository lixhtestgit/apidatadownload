using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PPPayReportTools.Excel
{
    /// <summary>
    /// 标题字段映射类
    /// </summary>
    internal class ExcelTitleFieldMapper
    {
        /// <summary>
        /// 属性信息
        /// </summary>
        public PropertyInfo PropertyInfo { get; set; }
        /// <summary>
        /// 行标题
        /// </summary>
        public string ExcelTitle { get; set; }
        /// <summary>
        /// 行标题下标位置
        /// </summary>
        public int ExcelTitleIndex { get; set; }
        /// <summary>
        /// 是否要做行内容空检查
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
        /// 获取对应关系_T属性添加了标题映射关系
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<ExcelTitleFieldMapper> GetModelFieldMapper<T>()
        {
            List<ExcelTitleFieldMapper> fieldMapperList = new List<ExcelTitleFieldMapper>(100);

            List<PropertyInfo> tPropertyInfoList = typeof(T).GetProperties().ToList();
            ExcelTitleAttribute excelTitleAttribute = null;
            foreach (var tPropertyInfo in tPropertyInfoList)
            {
                excelTitleAttribute = (ExcelTitleAttribute)tPropertyInfo.GetCustomAttribute(typeof(ExcelTitleAttribute));
                if (excelTitleAttribute != null)
                {
                    fieldMapperList.Add(new ExcelTitleFieldMapper
                    {
                        PropertyInfo = tPropertyInfo,
                        ExcelTitle = excelTitleAttribute.RowTitle,
                        ExcelTitleIndex = excelTitleAttribute.RowTitleIndex,
                        IsCheckContentEmpty = excelTitleAttribute.IsCheckContentEmpty,
                        OutputFormat = excelTitleAttribute.OutputFormat,
                        IsCoordinateExpress = excelTitleAttribute.IsCoordinateExpress
                    });
                }
            }
            return fieldMapperList;
        }

        /// <summary>
        /// 获取对应关系_手动提供映射关系
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldNameAndShowNameDic"></param>
        /// <returns></returns>
        public static List<ExcelTitleFieldMapper> GetModelFieldMapper<T>(Dictionary<string, string> fieldNameAndShowNameDic)
        {
            List<ExcelTitleFieldMapper> fieldMapperList = new List<ExcelTitleFieldMapper>(100);

            List<PropertyInfo> tPropertyInfoList = typeof(T).GetProperties().ToList();
            PropertyInfo propertyInfo = null;

            foreach (var item in fieldNameAndShowNameDic)
            {
                propertyInfo = tPropertyInfoList.Find(m => m.Name.Equals(item.Key, StringComparison.OrdinalIgnoreCase));

                fieldMapperList.Add(new ExcelTitleFieldMapper
                {
                    PropertyInfo = propertyInfo,
                    ExcelTitle = item.Value,
                    ExcelTitleIndex = -1,
                    OutputFormat = null,
                    IsCheckContentEmpty = false,
                    IsCoordinateExpress = false
                });
            }
            return fieldMapperList;
        }

        /// <summary>
        /// 获取对应关系_未提供（默认属性名和标题名一致）
        /// </summary>
        /// <returns></returns>
        public static List<ExcelTitleFieldMapper> GetModelDefaultFieldMapper<T>()
        {
            List<ExcelTitleFieldMapper> fieldMapperList = new List<ExcelTitleFieldMapper>(100);

            List<PropertyInfo> tPropertyInfoList = typeof(T).GetProperties().ToList();

            foreach (var item in tPropertyInfoList)
            {
                fieldMapperList.Add(new ExcelTitleFieldMapper
                {
                    PropertyInfo = item,
                    ExcelTitle = item.Name,
                    ExcelTitleIndex = -1,
                    OutputFormat = null,
                    IsCheckContentEmpty = false,
                    IsCoordinateExpress = false
                });
            }
            return fieldMapperList;
        }

    }
}
