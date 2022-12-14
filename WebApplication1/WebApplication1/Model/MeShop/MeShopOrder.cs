using System.Collections.Generic;
using System;
using WebApplication1.Helper;

namespace WebApplication1.Model.MeShop
{
	/// <summary>
	/// 店铺订单
	/// </summary>
	public class MeShopOrder
	{
		public int ID { get; set; }
		public DateTime CreateTime { get; set; }
		public string Email { get; set; }
		public int State { get; set; }
		public decimal CurrencyTotalPayPrice { get; set; }
		public decimal ShipPrice { get; set; }

		public List<MeShopOrderDetail> OrderItemList { get; set; }
	}
}
