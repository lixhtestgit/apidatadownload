using System.ComponentModel;

namespace WebApplication1.Enum
{

    public class EOrder
    {
        /// <summary>
        /// 订单状态
        /// </summary>
        [Description("订单状态")]
        public enum State
        {
            /// <summary>
            /// 隐藏订单（用于点支付后支付未完成状态）
            /// </summary>
            [Description("hide")]
            隐藏 = -1,
            /// <summary>
            /// 未付款
            /// </summary>
            [Description("await_pay")]
            未付款 = 0,
            /// <summary>
            /// 已付款待处理
            /// </summary>
            [Description("verify_pay")]
            待处理 = 1,
            /// <summary>
            /// 已付款
            /// </summary>
            [Description("paied")]
            已付款 = 2,
            /// <summary>
            /// 部分付款
            /// </summary>
            [Description("partial_pay")]
            部分付款 = 3,
            /// <summary>
            /// 已完成
            /// </summary>
            [Description("completed")]
            已完成 = 4,
            /// <summary>
            /// 已取消
            /// </summary>
            [Description("cancelled")]
            已取消 = 5,
            /// <summary>
            /// 支付失败
            /// </summary>
            [Description("failed")]
            支付失败 = 6,
            /// <summary>
            /// 到付待发货 待确认
            /// 货到付款方式独有订单状态
            /// </summary>
            [Description("pay_confirm")]
            到付待确认 = 7
        }
        /// <summary>
        /// 发货状态
        /// </summary>
        [Description("发货状态")]
        public enum ShipState
        {
            /// <summary>
            /// 未发货
            /// </summary>
            [Description("not_shipped")]
            未发货 = 0,
            /// <summary>
            /// 部分发货
            /// </summary>
            [Description("partial_shipped")]
            部分发货 = 1,
            /// <summary>
            /// 已发货
            /// </summary>
            [Description("shipped")]
            已发货 = 2
        }
        /// <summary>
        /// 退款状态
        /// </summary>
        [Description("退款状态")]
        public enum RefundState
        {
            /// <summary>
            /// 未退款
            /// </summary>
            [Description("not_refunded")]
            未退款 = 0,
            /// <summary>
            /// 部分退款
            /// </summary>
            [Description("partial_refunded")]
            部分退款 = 1,
            /// <summary>
            /// 已退款
            /// </summary>
            [Description("refunded")]
            已退款 = 2,
            /// <summary>
            /// 退款处理中
            /// </summary>
            [Description("refunded_processing")]
            退款处理中 = 3
        }
        /// <summary>
        /// 是否是无注册购买
        /// </summary>
        [Description("是否是无注册购买")]
        public enum IsGuest
        {
            /// <summary>
            /// 否
            /// </summary>
            [Description("no")]
            否 = 0,
            /// <summary>
            /// 是
            /// </summary>
            [Description("yes")]
            是 = 1,
        }
        /// <summary>
        /// 是否发送邮件
        /// </summary>
        [Description("是否发送邮件")]
        public enum IsSendEmail
        {
            /// <summary>
            /// 未发送
            /// </summary>
            [Description("no")]
            未发送 = 0,
            /// <summary>
            /// 已发送
            /// </summary>
            [Description("yes")]
            已发送 = 1
        }
        /// <summary>
        /// 是否是无注册订单
        /// </summary>
        [Description("是否是无注册订单")]
        public enum IsNoregister
        {
            /// <summary>
            /// 已注册
            /// </summary>
            [Description("registered")]
            已注册 = 0,
            /// <summary>
            /// 未注册
            /// </summary>
            [Description("unregistered")]
            无注册 = 1
        }
        /// <summary>
        /// 创建类型
        /// </summary>
        public enum CreateType
        {
            [Description("front")]
            前台订单 = 0,
            [Description("backgroud")]
            后台订单 = 1
        }
    }

}
