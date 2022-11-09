using PPPayReportTools.Excel;
using System;
using System.Text.RegularExpressions;
using WebApplication1.Extension;

namespace WebApplication1.Model.PayNotify
{
    public class ShopOrder
    {
        /// <summary>
        /// 会话ID
        /// </summary>
        [ExcelTitle("SessionID")]
        public string SessionID { get; set; }

        /// <summary>
        /// 站点编码
        /// </summary>
        [ExcelTitle("SiteCode")]
        public string SiteCode { get; set; }

        /// <summary>
        /// 订单编码
        /// </summary>
        [ExcelTitle("OrderCode")]
        public string OrderCode { get; set; }

        /// <summary>
        /// 订单创建时间
        /// </summary>
        public string CreateTime { get; set; }

        /// <summary>
        /// 订单创建时间
        /// </summary>
        [ExcelTitle("CreateTime(北京)")]
        public DateTime ShowCreateTime
        {
            get
            {
                DateTime createTime = DateTime.MinValue;
                if (this.CreateTime.IsNotNullOrEmpty())
                {
                    DateTime.TryParse(this.CreateTime, out createTime);
                }
                return createTime;
            }
            set
            {
                this.CreateTime = value.ToString_yyyyMMddHHmmss();
            }
        }

        /// <summary>
        /// 订单支付金额
        /// </summary>
        public string TotalPayPrice { get; set; }

        /// <summary>
        /// 订单支付金额
        /// </summary>
        [ExcelTitle("TotalPayPrice")]
        public decimal ShowTotalPayPrice
        {
            get
            {
                decimal totalPayPrice = 0;
                if (this.TotalPayPrice.IsNotNullOrEmpty())
                {
                    decimal.TryParse(this.TotalPayPrice, out totalPayPrice);
                }
                return totalPayPrice;
            }
            set
            {
                this.TotalPayPrice = value.ToString();
            }
        }

        /// <summary>
        /// 订单交易号
        /// </summary>
        [ExcelTitle("TX")]
        public string TX { get; set; }

        /// <summary>
        /// 产品信息
        /// </summary>
        [ExcelTitle("Product")]
        public string Product { get; set; }

        /// <summary>
        /// 用户邮箱
        /// </summary>
        [ExcelTitle("Email")]
        public string Email { get; set; }

        /// <summary>
        /// 用户电话
        /// </summary>
        [ExcelTitle("Phone")]
        public string Phone { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [ExcelTitle("FirstName")]
        public string FirstName { get; set; }

        /// <summary>
        /// 用户姓
        /// </summary>
        [ExcelTitle("LastName")]
        public string LastName { get; set; }

        /// <summary>
        /// 国家
        /// </summary>
        [ExcelTitle("Country")]
        public string Country { get; set; }

        /// <summary>
        /// 省州
        /// </summary>
        [ExcelTitle("Province")]
        public string Province { get; set; }

        /// <summary>
        /// 订单交易号
        /// </summary>
        [ExcelTitle("City")]
        public string City { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [ExcelTitle("Address")]
        public string Address { get; set; }

        /// <summary>
        /// 邮编
        /// </summary>
        [ExcelTitle("Zip")]
        public string Zip { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [ExcelTitle("Remark")]
        public string Remark { get; set; }

    }
}
