using PPPayReportTools.Excel;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication1.Model
{
    public class CheckoutOrder
    {
        public CheckoutOrder()
        {
            this.ESPayTypeList = new List<string>();
            this.SessionIDList = new List<string>();
            this.CreateOrderResultList = new List<string>();
            this.PayResultList = new List<string>();
            this.ESCreateOrderResultLogList = new List<string>();
            this.ESPayResultLogList = new List<string>();
        }

        [ExcelTitle("弃单号")]
        public string CheckoutGuid { get; set; }

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

        [ExcelTitle("创建订单结果")]
        public string CreateOrderResultShow
        {
            get
            {
                if (this.CreateOrderResultList == null || this.CreateOrderResultList.Count == 0)
                {
                    return "无";
                }
                return string.Join(",", this.CreateOrderResultList.Distinct());
            }
        }
        public List<string> CreateOrderResultList { get; set; }

        [ExcelTitle("订单支付结果")]
        public string PayResultShow
        {
            get
            {
                if (this.PayResultList == null || this.PayResultList.Count == 0)
                {
                    return "无";
                }
                return string.Join(",", this.PayResultList.Distinct());
            }
        }
        public List<string> PayResultList { get; set; }

        [ExcelTitle("创建订单结果原始日志")]
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

        [ExcelTitle("支付结果原始日志")]
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
