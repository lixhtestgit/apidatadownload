using System;
using System.Collections.Generic;

namespace WebApplication1.Model.MeShop
{
	/// <summary>
	/// 店铺订单
	/// </summary>
	public class MeShopOrder
	{
		/// <summary>
		/// 订单ID
		/// </summary>
		public long ID { get; set; }
		/// <summary>
		/// 创建时间
		/// </summary>
		public DateTime CreateTime { get; set; }
		/// <summary>
		/// 用户邮箱
		/// </summary>
		public string Email { get; set; }
        /// <summary>
        /// 订单状态(隐藏 = -1,未付款 = 0,待处理 = 1,已付款 = 2,部分付款 = 3,已完成 = 4,已取消 = 5,支付失败 = 6,到付待确认 = 7)
        /// </summary>
        public int State { get; set; }
		/// <summary>
		/// 多币种支付金额
		/// </summary>
		public decimal CurrencyTotalPayPrice { get; set; }
		/// <summary>
		/// 运费金额
		/// </summary>
		public decimal ShipPrice { get; set; }
        /// <summary>
        /// 发货状态(未发货 = 0,部分发货 = 1,已发货 = 2)
        /// </summary>
        public int ShipState { get; set; }
        /// <summary>
        /// 子单列表
        /// </summary>
        public List<MeShopOrderDetail> OrderItemList { get; set; }
	}
}
