using System.ComponentModel;

namespace WebApplication1.Enum
{

    /// <summary>
    /// 产品集合类型
    /// </summary>
    public enum EMeShopProductCollectionType
    {
        [Description("automatic")]
        自动 = 0,
        [Description("manual")]
        手动 = 1
    }

}
