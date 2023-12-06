namespace WebApplication1.Model.MeShop
{
    /// <summary>
    /// 子单
    /// </summary>
	public class MeShopOrderItem
	{
        /// <summary>
        /// ID
        /// </summary>
        public long ID { get; set; }
        /// <summary>
        /// 订单ID
        /// </summary>
        public long OrderID { get; set; }
        /// <summary>
        /// SPUID
        /// </summary>
        public long SPUID { get; set; }
        /// <summary>
        /// SKUID
        /// </summary>
        public long SKUID { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// 产品标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// SKU售卖价格
        /// </summary>
        public decimal SellPrice { get; set; }
        /// <summary>
        /// 发货状态(未发货 = 0,部分发货 = 1,已发货 = 2)
        /// </summary>
        public int ShipState { get; set; }
        /// <summary>
        /// 退货状态
        /// </summary>
        public int RefundState { get; set; }
    }
}
