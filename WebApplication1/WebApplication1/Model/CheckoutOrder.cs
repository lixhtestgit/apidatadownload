using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication1.Model
{
    public class CheckoutOrder
    {
        /// <summary>
        /// 弃单ID
        /// </summary>
        public string CheckoutID { get; set; }

        [ExcelTitle("弃单号")]
        public string CheckoutGuid { get; set; }

        [ExcelTitle("姓名")]
        public string UserName { get; set; }

        [ExcelTitle("邮箱")]
        public string Email { get; set; }

        [ExcelTitle("国家")]
        public string CountryName { get; set; }

        [ExcelTitle("省州")]
        public string Province { get; set; }

        [ExcelTitle("城市")]
        public string City { get; set; }

        [ExcelTitle("详细地址")]
        public string Address { get; set; }

        [ExcelTitle("邮编")]
        public string ZIP { get; set; }

        [ExcelTitle("创建时间", outputFormat: "yyyy-MM-dd HH:mm:ss")]
        public DateTime CreateTime { get; set; }

        [ExcelTitle("电话")]
        public string Phone { get; set; }

        [ExcelTitle("币种")]
        public string ChoiseCurrency { get; set; }

        [ExcelTitle("金额")]
        public decimal CurrencyTotalPayPrice { get; set; }

        [ExcelTitle("产品金额")]
        public string ProductPrice
        {
            get
            {
                if (this.ProductPriceList == null || this.ProductPriceList.Count == 0)
                {
                    return "无";
                }
                return string.Join("\n", this.ProductPriceList.Distinct());
            }
        }
        public List<string> ProductPriceList { get; set; }

        [ExcelTitle("产品地址")]
        public string ProductUrl
        {
            get
            {
                if (this.ProductUrlList == null || this.ProductUrlList.Count == 0)
                {
                    return "无";
                }
                return string.Join("\n", this.ProductUrlList.Distinct());
            }
        }

        public List<string> ProductUrlList { get; set; }

        [ExcelTitle("恢复状态")]
        public string OrderState { get; set; }

        [ExcelTitle("支付方式")]
        public string ESPayType
        {
            get
            {
                if (this.ESPayTypeList == null || this.ESPayTypeList.Count == 0)
                {
                    return "无";
                }
                return string.Join(",", this.ESPayTypeList.Distinct());
            }
        }
        public List<string> ESPayTypeList { get; set; }

        [ExcelTitle("SessionID")]
        public string SessionID
        {
            get
            {
                if (this.SessionIDList == null || this.SessionIDList.Count == 0)
                {
                    return "无";
                }
                return string.Join(",", this.SessionIDList.Distinct());
            }
        }
        public List<string> SessionIDList { get; set; }

        [ExcelTitle("创建订单失败原因")]
        public string CreateOrderErrorReason
        {
            get
            {
                if (this.CreateOrderErrorReasonList == null || this.CreateOrderErrorReasonList.Count == 0)
                {
                    return "无";
                }
                return string.Join(",", this.CreateOrderErrorReasonList.Distinct());
            }
        }
        public List<string> CreateOrderErrorReasonList { get; set; }

        [ExcelTitle("支付失败原因")]
        public string PayErrorReason
        {
            get
            {
                if (this.PayErrorReasonList == null || this.PayErrorReasonList.Count == 0)
                {
                    return "无";
                }
                return string.Join(",", this.PayErrorReasonList.Distinct());
            }
        }
        public List<string> PayErrorReasonList { get; set; }

        [ExcelTitle("创建订单结果日志")]
        public string ESCreateOrderResultLog
        {
            get
            {
                if (this.ESCreateOrderResultLogList == null || this.ESCreateOrderResultLogList.Count == 0)
                {
                    return "无";
                }
                return string.Join("\n", this.ESCreateOrderResultLogList.Distinct());
            }
        }
        public List<string> ESCreateOrderResultLogList { get; set; }

        [ExcelTitle("支付结果日志")]
        public string ESPayResultLog
        {
            get
            {
                if (this.ESPayResultLogList == null || this.ESPayResultLogList.Count == 0)
                {
                    return "无";
                }
                return string.Join("\n", this.ESPayResultLogList.Distinct());
            }
        }
        public List<string> ESPayResultLogList { get; set; }
    }
}
