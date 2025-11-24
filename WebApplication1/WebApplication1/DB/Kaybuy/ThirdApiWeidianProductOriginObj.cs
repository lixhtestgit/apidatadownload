using System.Collections.Generic;

namespace WebApplication1.DB.Kaybuy
{
    /// <summary>
    /// 通用三方产品详情类（基于淘宝封装）
    /// </summary>
    public class ThirdApiProductDetailDto
    {
        /// <summary>
        /// 店铺ID
        /// </summary>
        public string ShopID { get; set; } = string.Empty;

        /// <summary>
        /// 店铺名称
        /// </summary>
        public string ShopName { get; set; } = string.Empty;

        /// <summary>
        /// 商品ID
        /// </summary>
        public string ItemId { get; set; } = string.Empty;

        /// <summary>
        /// 商品名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 适用价格（元）
        /// </summary>
        public decimal ApplicablePrice { get; set; }

        /// <summary>
        /// 主图地址
        /// </summary>
        public string? MainImageUrl { get; set; }

        /// <summary>
        /// 商品图片列表
        /// </summary>
        public List<string> PicUrls { get; set; } = new List<string>();

        /// <summary>
        /// 商品选项（如颜色、尺码等）
        /// </summary>
        public List<ThirdApiProductDetailDto_ItemOption> ItemOptions { get; set; } = new List<ThirdApiProductDetailDto_ItemOption>();

        /// <summary>
        /// SKU列表
        /// </summary>
        public List<ThirdApiProductDetailDto_Sku> Skus { get; set; } = new List<ThirdApiProductDetailDto_Sku>();

        /// <summary>
        /// 商品描述信息
        /// </summary>
        public string Information { get; set; } = string.Empty;

        /// <summary>
        /// 产品地址
        /// </summary>
        public string ProductUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// 淘宝产品详情项目选项类
    /// </summary>
    public class ThirdApiProductDetailDto_ItemOption
    {
        /// <summary>
        /// 选项ID
        /// </summary>
        public string OptionID { get; set; } = string.Empty;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 选择列表
        /// </summary>
        public List<ThirdApiProductDetailDto_ItemOptionChoice> ChoiceList { get; set; } = new List<ThirdApiProductDetailDto_ItemOptionChoice>();
    }

    /// <summary>
    /// 淘宝产品详情项目选项选择类
    /// </summary>
    public class ThirdApiProductDetailDto_ItemOptionChoice
    {
        /// <summary>
        /// 选项ID
        /// </summary>
        public string ChoiceID { get; set; } = string.Empty;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 资源链接(图片链接)
        /// </summary>
        public string Src { get; set; } = string.Empty;
    }

    /// <summary>
    /// 淘宝产品详情选项库存价格类
    /// </summary>
    public class ThirdApiProductDetailDto_Sku
    {
        /// <summary>
        /// SKU ID
        /// </summary>
        public string SkuId { get; set; } = string.Empty;

        /// <summary>
        /// 库存数量
        /// </summary>
        public int StockQuantity { get; set; }

        /// <summary>
        /// 价格
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 商品属性
        /// </summary>
        public List<ThirdApiProductDetailDto_Property> Properties { get; set; } = new List<ThirdApiProductDetailDto_Property>();
    }

    /// <summary>
    /// 淘宝产品详情属性
    /// </summary>
    public class ThirdApiProductDetailDto_Property
    {
        /// <summary>
        /// 选项卡ID
        /// </summary>
        public string OptionID { get; set; } = string.Empty;

        /// <summary>
        /// 选项卡名称
        /// </summary>
        public string OptionName { get; set; } = string.Empty;

        /// <summary>
        /// 选项ID
        /// </summary>
        public string ChoiceID { get; set; } = string.Empty;

        /// <summary>
        /// 选项名称
        /// </summary>
        public string ChoiceName { get; set; } = string.Empty;
    }
}
