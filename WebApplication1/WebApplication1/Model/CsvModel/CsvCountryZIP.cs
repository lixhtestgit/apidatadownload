using System.Collections.Generic;
using WebApplication1.ExcelCsv;

namespace WebApplication1.Model.CsvModel
{
    /// <summary>
    /// 国家邮编
    /// </summary>
    public class CsvCountryZIP
    {
        /// <summary>
        /// 邮编
        /// </summary>
        [CsvColumn("zip")]
        public string ZIP { get; set; }

        /// <summary>
        /// 省州简码
        /// </summary>
        [CsvColumn("state_id")]
        public string ProvinceCode { get; set; }

        /// <summary>
        /// 省州名称
        /// </summary>
        [CsvColumn("state_name")]
        public string ProvinceName { get; set; }

        ///// <summary>
        ///// 国家简码
        ///// </summary>
        //[CsvColumn("COUNTRY")]
        //public string CountryCode { get; set; }
    }


    public class CountryZIPData
    {
        /// <summary>
        /// 省州简码
        /// </summary>
        public string ProvinceCode { get; set; }

        /// <summary>
        /// 省州名称
        /// </summary>
        public string ProvinceName { get; set; }

        /// <summary>
        /// 邮编列表
        /// </summary>
        public List<CountryZIPData_ZIP> ZipList { get; set; }
    }
    public class CountryZIPData_ZIP
    {
        public string StartFlag { get; set; }
        public string MinZip { get; set; }
        public string MaxZip { get; set; }
    }
}
