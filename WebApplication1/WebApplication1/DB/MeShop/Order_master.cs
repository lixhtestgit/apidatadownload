using System;
using WebApplication1.DB.Extend;

namespace WebApplication1.DB.MeShop
{
    /// <summary>
    /// 主单
    /// </summary>
    [ClassMapper(Enum.EDBSiteName.Wigsbuyshop, "", "Order_master")]
    public class Order_master
    {
        /// <summary>
        /// 主键
        /// </summary>
        [PropertyMapper(isPrimaryKey: true, isIdentityInsert: true)]
        public long ID { get; set; }

        /// <summary>
        /// 创建类型
        /// </summary>
        [PropertyMapper]
        public string CreateType { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [PropertyMapper]
        public string UserID { get; set; }

        /// <summary>
        /// 结账ID
        /// </summary>
        [PropertyMapper]
        public string CheckoutID { get; set; }

        /// <summary>
        /// 支付渠道ID
        /// </summary>
        [PropertyMapper]
        public string PayChannelID { get; set; }

        /// <summary>
        /// 支付账户ID
        /// </summary>
        [PropertyMapper]
        public string PayAccountID { get; set; }

        /// <summary>
        /// 支付账户名称
        /// </summary>
        [PropertyMapper]
        public string PayAccountName { get; set; }

        /// <summary>
        /// 会话ID
        /// </summary>
        [PropertyMapper]
        public string SessionID { get; set; }

        /// <summary>
        ///  shipping ID
        /// </summary>
        [PropertyMapper]
        public string ShipID { get; set; }

        /// <summary>
        /// shipping名称
        /// </summary>
        [PropertyMapper]
        public string ShipName { get; set; }

        /// <summary>
        /// 总支付价格
        /// </summary>
        [PropertyMapper]
        public decimal TotalPayPrice { get; set; }

        /// <summary>
        /// 总原始价格
        /// </summary>
        [PropertyMapper]
        public decimal TotalOriginalPrice { get; set; }

        /// <summary>
        /// shipping价格
        /// </summary>
        [PropertyMapper]
        public decimal ShipPrice { get; set; }

        /// <summary>
        /// 税费价格
        /// </summary>
        [PropertyMapper]
        public decimal TaxPrice { get; set; }

        /// <summary>
        /// 总折扣
        /// </summary>
        [PropertyMapper]
        public decimal TotalDiscount { get; set; }

        /// <summary>
        /// 总支付公司折扣
        /// </summary>
        [PropertyMapper]
        public decimal TotalPayCompanyDiscount { get; set; }

        /// <summary>
        /// 优惠券代码
        /// </summary>
        [PropertyMapper]
        public string CouponCode { get; set; }

        /// <summary>
        /// 退款价格
        /// </summary>
        [PropertyMapper]
        public decimal RefundPrice { get; set; }

        /// <summary>
        /// 退款货币价格
        /// </summary>
        [PropertyMapper]
        public decimal RefundCurrencyPrice { get; set; }

        /// <summary>
        /// 货币
        /// </summary>
        [PropertyMapper]
        public string Currency { get; set; }

        /// <summary>
        /// 货币符号
        /// </summary>
        [PropertyMapper]
        public string CurrencySymbol { get; set; }

        /// <summary>
        /// 货币汇率
        /// </summary>
        [PropertyMapper]
        public decimal CurrencyRate { get; set; }

        /// <summary>
        /// 选择货币
        /// </summary>
        [PropertyMapper]
        public string ChoiseCurrency { get; set; }

        /// <summary>
        /// 选择货币汇率
        /// </summary>
        [PropertyMapper]
        public decimal ChoiseCurrencyRate { get; set; }

        /// <summary>
        /// 选择货币符号
        /// </summary>
        [PropertyMapper]
        public string ChoiseCurrencySymbol { get; set; }

        /// <summary>
        /// 货币总支付价格
        /// </summary>
        [PropertyMapper]
        public decimal CurrencyTotalPayPrice { get; set; }

        /// <summary>
        /// 履行状态
        /// </summary>
        [PropertyMapper]
        public string FulfillmentState { get; set; }

        /// <summary>
        /// 退款状态
        /// </summary>
        [PropertyMapper]
        public string RefundState { get; set; }

        /// <summary>
        /// 取消时间
        /// </summary>
        [PropertyMapper]
        public DateTime? CancelTime { get; set; }

        /// <summary>
        /// 取消原因
        /// </summary>
        [PropertyMapper]
        public string CancelReason { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        [PropertyMapper]
        public DateTime? CompleteTime { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        [PropertyMapper]
        public string IP { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [PropertyMapper]
        public string Note { get; set; }

        /// <summary>
        /// 税种标题
        /// </summary>
        [PropertyMapper]
        public string TaxTitle { get; set; }

        /// <summary>
        /// 税种代码
        /// </summary>
        [PropertyMapper]
        public string TaxCode { get; set; }

        /// <summary>
        /// 交易号
        /// </summary>
        [PropertyMapper]
        public string TX { get; set; }

        /// <summary>
        /// 是否未注册
        /// </summary>
        [PropertyMapper]
        public bool IsNoRegister { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [PropertyMapper]
        public int State { get; set; }

        /// <summary>
        /// 流量来源
        /// </summary>
        [PropertyMapper]
        public string TrafficSource { get; set; }

        /// <summary>
        /// 同步到ES
        /// </summary>
        [PropertyMapper]
        public bool SyncToEs { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [PropertyMapper]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 支付时间
        /// </summary>
        [PropertyMapper]
        public DateTime? PayTime { get; set; }

        /// <summary>
        /// 同步时间
        /// </summary>
        [PropertyMapper]
        public DateTime? SyncTime { get; set; }

        /// <summary>
        /// 订单跟踪代码
        /// </summary>
        [PropertyMapper]
        public string OrderTrackCode { get; set; }
    }
}
