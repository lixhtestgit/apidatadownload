namespace WebApplication1.ExcelCsv
{
    /// <summary>
    /// Csv文件类特性标记
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false)]
    public class CsvColumnAttribute : System.Attribute
    {
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 字符输出格式（数字和日期类型需要）
        /// </summary>
        public string OutputFormat { get; set; }
        /// <summary>
        /// 单元格是否要检查空数据（true为检查，为空的行数据不添加）
        /// </summary>
        public bool IsCheckContentEmpty { get; set; }

        public CsvColumnAttribute(bool isCheckEmpty) : this(null, null, isCheckEmpty) { }

        public CsvColumnAttribute(string title) : this(title, null, false) { }

        public CsvColumnAttribute(string title, string outputFormat) : this(title, outputFormat, false) { }

        public CsvColumnAttribute(string title, bool isCheckEmpty) : this(title, null, isCheckEmpty) { }

        public CsvColumnAttribute(string title, string outputFormat, bool isCheckEmpty)
        {
            Title = title;
            OutputFormat = outputFormat;
            IsCheckContentEmpty = isCheckEmpty;
        }
    }
}
