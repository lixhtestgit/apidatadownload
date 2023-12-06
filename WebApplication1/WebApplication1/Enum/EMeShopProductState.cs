using System.ComponentModel;

namespace WebApplication1.Enum
{

    /// <summary>
    /// 产品状态枚举
    /// </summary>
    public enum EMeShopProductState
    {
        /// <summary>
        /// 删除
        /// </summary>
        [Description("deleted")]
        删除 = -1,
        /// <summary>
        /// 禁用
        /// </summary>
        [Description("disable")]
        下架 = 0,

        /// <summary>
        /// 启用
        /// </summary>
        [Description("enable")]
        上架 = 1
    }

}
