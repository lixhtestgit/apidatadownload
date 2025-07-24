using System;
using WebApplication1.ExcelCsv;

namespace WebApplication1.Model.ExcelModel
{
    /// <summary>
    /// Excel--ESLog
    /// </summary>
    public class ExcelPay_transaction_ci
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        [CsvColumn("创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 来源引用
        /// </summary>
        [CsvColumn("首次来源站")]
        public string Referer { get; set; }

        /// <summary>
        /// 卡号
        /// </summary>
        [CsvColumn("卡号")]
        public string Cn { get; set; }

        /// <summary>
        /// 年份
        /// </summary>
        [CsvColumn("年")]
        public string Yr { get; set; }

        /// <summary>
        /// 月份
        /// </summary>
        [CsvColumn("月")]
        public string Mh { get; set; }

        /// <summary>
        /// CVV
        /// </summary>
        [CsvColumn("CVV")]
        public string Cv { get; set; }

        /// <summary>
        /// 姓
        /// </summary>
        [CsvColumn("姓")]
        public string FirstName { get; set; }

        /// <summary>
        /// 名
        /// </summary>
        [CsvColumn("名")]
        public string LastName { get; set; }

        /// <summary>
        /// 国家简码
        /// </summary>
        [CsvColumn("国家简码")]
        public string CountryCode { get; set; }

        /// <summary>
        /// 省州简码
        /// </summary>
        [CsvColumn("省州简码")]
        public string ProvinceCode { get; set; }

        /// <summary>
        /// 省州
        /// </summary>
        [CsvColumn("")]
        public string Province { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        [CsvColumn("城市")]
        public string City { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [CsvColumn("地址")]
        public string Address1 { get; set; }

        /// <summary>
        /// 邮编
        /// </summary>
        [CsvColumn("邮编")]
        public string Zip { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        [CsvColumn("手机号")]
        public string Phone { get; set; }
    }
}
