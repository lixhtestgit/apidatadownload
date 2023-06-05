using PPPayReportTools.Excel;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication1.Model
{
    public class CheckoutOrder
    {
        public CheckoutOrder()
        {
            this.ESPayTypeList = new List<string>(1);
            this.SessionIDList = new List<string>(1);
            this.CreateOrderResultList = new List<string>(1);
            this.PayResultList = new List<string>(1);
            this.ESCreateOrderResultLogList = new List<string>(1);
            this.ESPayResultLogList = new List<string>(1);
            this.ESPayAccountList = new List<string>(1);
        }

        [ExcelTitle("弃单号")]
        public string CheckoutGuid { get; set; }

        [ExcelTitle("订单号")]
        public string OrderID { get; set; }

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
            set
            {
                this.ESPayTypeList = value.Split('\n').ToList();
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
            set
            {
                this.SessionIDList = value.Split('\n').ToList();
            }
        }
        public List<string> SessionIDList { get; set; }

        [ExcelTitle("收款账号")]
        public string ESPayAccount
        {
            get
            {
                if (this.ESPayAccountList == null || this.ESPayAccountList.Count == 0)
                {
                    return "无";
                }
                return string.Join(",", this.ESPayAccountList.Distinct());
            }
            set
            {
                this.ESPayAccountList = value.Split('\n').ToList();
            }
        }
        public List<string> ESPayAccountList { get; set; }

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
            set
            {
                this.CreateOrderResultList = value.Split('\n').ToList();
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
            set
            {
                this.PayResultList = value.Split('\n').ToList();
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
            set
            {
                this.ESCreateOrderResultLogList = value.Split('\n').ToList();
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
            set
            {
                this.ESPayResultLogList = value.Split('\n').ToList();
            }
        }
        public List<string> ESPayResultLogList { get; set; }
    }
}
