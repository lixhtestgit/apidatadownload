using PPPayReportTools.Excel;
using System;

namespace WebApplication1.Model.ExcelModel
{
	/// <summary>
	/// 统计_订单
	/// </summary>
	public class ExcelTBOrder
	{
		[ExcelTitle("ID")]
		public int OriginID { get; set; }
		[ExcelTitle("订单时间")]
		public DateTime AddTime { get; set; }
		[ExcelTitle("美金金额")]
		public decimal CurrencyPrice { get; set; }
	}
}
