using PPPayReportTools.Excel;
using System;

namespace WebApplication1.Model.ExcelModel
{
	/// <summary>
	/// Excel订单导入
	/// </summary>
	public class ExcelOrderImport
	{
		[ExcelTitle("客户姓名")]
		public string CustomerName { get; set; }

		[ExcelTitle("客户邮箱")]
		public string CustomerEmail { get; set; }

		[ExcelTitle("手机号")]
		public string CustomerPhone { get; set; }

		[ExcelTitle("订单号")]
		public long OrderID { get; set; }

		[ExcelTitle("子单号")]
		public long BillID { get; set; }

		[ExcelTitle("币种")]
		public string CurrencyName { get; set; }

		[ExcelTitle("订单合计金额")]
		public decimal OrderTotalPrice { get; set; }

		[ExcelTitle("运费")]
		public decimal OrderShipPrice { get; set; }

		[ExcelTitle("订单状态")]
		public string OrderState { get; set; }

		[ExcelTitle("备注")]
		public string OrderRemark { get; set; }

		[ExcelTitle("下单IP")]
		public string IP { get; set; }

		[ExcelTitle("支付时间(UTC+00:00)")]
		public DateTime PayTime { get; set; }

		[ExcelTitle("交易流水号")]
		public string TX { get; set; }

		[ExcelTitle("支付渠道")]
		public string PayChannel { get; set; }

		[ExcelTitle("国家/地区")]
		public string CountryName { get; set; }

		[ExcelTitle("省州")]
		public string Province { get; set; }

		[ExcelTitle("城市")]
		public string City { get; set; }

		[ExcelTitle("收货地址")]
		public string Address { get; set; }

		[ExcelTitle("邮编")]
		public string Zip { get; set; }

		[ExcelTitle("SPU")]
		public string SPU { get; set; }

		[ExcelTitle("SKU")]
		public string SKU { get; set; }

		[ExcelTitle("子单产品数量")]
		public int BillProductCount { get; set; }

		[ExcelTitle("子单金额")]
		public decimal BillPrice { get; set; }
	}
}
