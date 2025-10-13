using System;
using WebApplication1.DB.Extend;

namespace WebApplication1.DB.MeShop
{
    /// <summary>
    /// 子单
    /// </summary>
    [ClassMapper(Enum.EDBSiteName.Wigsbuyshop, "", "Order_item")]
    public class Order_item
	{
        /// <summary>
        /// 主键
        /// </summary>
        [PropertyMapper(isPrimaryKey: true, isIdentityInsert: true)]
        public long ID { get; set; }

        /// <summary>
        /// SPU ID
        /// </summary>
        [PropertyMapper]
        public Guid SpuId { get; set; }

        /// <summary>
        /// SKU ID
        /// </summary>
        [PropertyMapper]
        public Guid SkuId { get; set; }

        /// <summary>
        /// 订单ID
        /// </summary>
        [PropertyMapper]
        public long OrderID { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [PropertyMapper]
        public int Count { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        [PropertyMapper]
        public string Title { get; set; }

        /// <summary>
        /// SPU编号
        /// </summary>
        [PropertyMapper]
        public string SPU { get; set; }

        /// <summary>
        /// SKU编号
        /// </summary>
        [PropertyMapper]
        public string SKU { get; set; }

        /// <summary>
        /// 选项文本
        /// </summary>
        [PropertyMapper]
        public string OptionText { get; set; }

        /// <summary>
        /// 属性
        /// </summary>
        [PropertyMapper]
        public string Properties { get; set; }

        /// <summary>
        /// 来源
        /// </summary>
        [PropertyMapper]
        public string Src { get; set; }

        /// <summary>
        /// 商品类型
        /// </summary>
        [PropertyMapper]
        public int ItemType { get; set; }

        /// <summary>
        /// 克重
        /// </summary>
        [PropertyMapper]
        public decimal Grams { get; set; }

        /// <summary>
        /// 销售价格
        /// </summary>
        [PropertyMapper]
        public decimal SellPrice { get; set; }

        /// <summary>
        /// 分摊支付价格
        /// </summary>
        [PropertyMapper]
        public decimal SplitPayPrice { get; set; }

        /// <summary>
        /// 重量
        /// </summary>
        [PropertyMapper]
        public decimal Weight { get; set; }

        /// <summary>
        /// 重量单位
        /// </summary>
        [PropertyMapper]
        public int WeightUnit { get; set; }

        /// <summary>
        /// 履行状态
        /// </summary>
        [PropertyMapper]
        public int FulfillmentState { get; set; }

        /// <summary>
        /// 退款状态
        /// </summary>
        [PropertyMapper]
        public int RefundState { get; set; }
    }
}
