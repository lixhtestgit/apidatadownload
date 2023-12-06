using System.Collections.Generic;
using System;

namespace WebApplication1.Model.MeShop
{
    /// <summary>
    /// 主单详情
    /// </summary>
	public class MeShopOrderDetail
	{
        /// <summary>
        /// 订单ID
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 用户邮箱
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// 订单状态
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
        /// 子单列表
        /// </summary>
        public List<MeShopOrderItem> OrderItemList { get; set; }
    }
}
