using System.Text;

namespace WebApplication1.ExcelCsv
{
    public class CsvFileDescription
    {
        public CsvFileDescription() : this(0)
        {
        }
        public CsvFileDescription(int titleRawIndex) : this(',', titleRawIndex, Encoding.UTF8)
        {
        }
        public CsvFileDescription(char separatorChar, int titleRawIndex, Encoding encoding)
        {
            SeparatorChar = separatorChar;
            TitleRawIndex = titleRawIndex;
            Encoding = encoding;
        }

        /// <summary>
        /// CSV文件字符编码
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// 分隔符（默认为（,）,也可以是其他分隔符如(\t)）
        /// </summary>
        public char SeparatorChar { get; set; }
        /// <summary>
        /// 标题所在行位置（默认为0，没有标题填-1）
        /// </summary>
        public int TitleRawIndex { get; set; }

    }
}
