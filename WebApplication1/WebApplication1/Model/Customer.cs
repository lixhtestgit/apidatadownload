using PPPayReportTools.Excel;

namespace WebApplication1.Model
{
    public class Customer
    {
        [ExcelTitle("推广科目")]
        public string ExtendSubject { get; set; }

        [ExcelTitle("推广来源")]
        public string Method { get; set; }

        [ExcelTitle("意向科目")]
        public string SubjectName { get; set; }

        [ExcelTitle("客户ID")]
        public string CustomerID { get; set; }

        [ExcelTitle("学员姓名")]
        public string RealName { get; set; }

        [ExcelTitle("客户电话ID")]
        public string Tel { get; set; }

        [ExcelTitle("手机号")]
        public string Phone { get; set; }

        [ExcelTitle("客户状态")]
        public string Result { get; set; }

        [ExcelTitle("跟进状态")]
        public string ConnectionStatusStr
        {
            get
            {
                if (this.ConnectionStatus == 1)
                {
                    return "未跟进";
                }
                return "已跟进";
            }
            set
            {
                this.ConnectionStatus = 0;
                if (value == "未跟进")
                {
                    this.ConnectionStatus = 1;
                }
            }
        }

        public int ConnectionStatus { get; set; }

        private string _customerType { get; set; }
        [ExcelTitle("客户类型")]
        public string CustomerType
        {
            get
            {
                return this._customerType;
            }
            set
            {
                this._customerType = "普通数据";
            }
        }

        [ExcelTitle("创建时间")]
        public string CreatedDate { get; set; }

        [ExcelTitle("重复时间")]
        public string RepeatDate { get; set; }

        [ExcelTitle("最近登录时间")]
        public string LastLoginTime { get; set; }

        [ExcelTitle("最近一次跟进时间")]
        public string LastNextStepDate { get; set; }

        [ExcelTitle("跟进次数")]
        public string ActivityCount { get; set; }

    }
}
