using PPPayReportTools.Excel;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication1.Model
{
    /// <summary>
    /// 订单发货类
    /// </summary>
    public class OrderShip
    {
        [ExcelTitle("订单号")]
        public string OrderID { get; set; }

        [ExcelTitle("发货渠道")]
        public string FreightName
        {
            get
            {
                if (this.FreightNameList == null || this.FreightNameList.Count == 0)
                {
                    return "无";
                }
                return string.Join(",", this.FreightNameList.Distinct());
            }
        }

        public List<string> FreightNameList { get; set; }
    }
}
