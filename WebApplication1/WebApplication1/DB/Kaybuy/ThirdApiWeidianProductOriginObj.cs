using System.Collections.Generic;

namespace WebApplication1.DB.Kaybuy
{
    /// <summary>
    /// 微店产品原始详情类
    /// </summary>
    public class ThirdApiWeidianProductOriginObj
    {
        /// <summary>
        /// 详情
        /// </summary>
        public ThirdApiWeidianProductOriginObj_Item Item { get; set; } = new ThirdApiWeidianProductOriginObj_Item();
    }

    /// <summary>
    /// 微店产品原始详情项目类
    /// </summary>
    public class ThirdApiWeidianProductOriginObj_Item
    {
        /// <summary>
        /// 宝贝ID
        /// </summary>
        public string Num_iid { get; set; } = string.Empty;

        /// <summary>
        /// 产品标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 价格
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 产品图列表
        /// </summary>
        /** 
        [
            {
              "url": "//img.alicdn.com/imgextra/i4/2596264565/TB2p30elFXXXXXQXpXXXXXXXXXX_!!2596264565.jpg"
            }
        ]
        */
        public List<ThirdApiWeidianProductOriginObj_Image> Item_imgs { get; set; } = new List<ThirdApiWeidianProductOriginObj_Image>(0);

        /// <summary>
        /// 商品属性图片列表
        /// </summary>
        /** 
        {
            "prop_img": [
              {
                "properties": "1627207:1347647754",
                "url": "//img.alicdn.com/imgextra/i3/2596264565/TB2.XeblVXXXXXkXpXXXXXXXXXX_!!2596264565.jpg"
              }
            ]
        }
        */
        public ThirdApiWeidianProductOriginObj_Propimgs Prop_imgs { get; set; } = new ThirdApiWeidianProductOriginObj_Propimgs();
        /// <summary>
        /// 商品属性
        /// </summary>
        /** 
        {
            "1627207:1347647754": "颜色分类:长方形带开瓶器+送工具刀卡+链子"
        }
        */
        public object Props_list { get; set; } = new object();
        /// <summary>
        /// 销量
        /// </summary>
        public string Sales { get; set; } = string.Empty;
        /// <summary>
        /// 卖家信息
        /// </summary>
        /** 
        {
            "title": "欢乐购客栈",
            "shop_name": "欢乐购客栈",
            "sid": "127203758",
            "zhuy": "//shop127203758.taobao.com",
            "shop_type": "C",
            "user_num_id": "2596264565",
            "nick": "欢乐购客栈"
          }
        */
        public ThirdApiWeidianProductOriginObj_Sellerinfo Seller_info { get; set; } = null!;
        /// <summary>
        /// 卖家ID
        /// </summary>
        public string Seller_id { get; set; } = string.Empty;
        /// <summary>
        /// 店铺ID
        /// </summary>
        public string Shop_id { get; set; } = string.Empty;
        /// <summary>
        /// Sku
        /// </summary>
        /** 
        {
            "sku":[
              {
                "price": "39",
                "orginal_price": "39.00",
                "properties": "1627207:1347647754",
                "properties_name": "1627207:1347647754:颜色分类:长方形带开瓶器+送工具刀卡+链子",
                "quantity": "104",
                "sku_id": "3166598625985"
              }
            ]
        }
        */
        public ThirdApiWeidianProductOriginObj_Skus Skus { get; set; } = null!;

        /// <summary>
        /// 描述
        /// </summary>
        public string Desc { get; set; } = string.Empty;

        /// <summary>
        /// 原始地址
        /// </summary>
        public string Detail_url { get; set; } = string.Empty;
    }

    /// <summary>
    /// 微店产品原始详情SKU类
    /// </summary>
    public class ThirdApiWeidianProductOriginObj_Skus
    {
        /// <summary>
        /// Sku详情列表
        /// </summary>
        public List<ThirdApiWeidianProductOriginObj_Skus_Detail> Sku { get; set; } = new List<ThirdApiWeidianProductOriginObj_Skus_Detail>(0);
    }

    /// <summary>
    /// 微店产品原始详情SKU详情类
    /// </summary>
    public class ThirdApiWeidianProductOriginObj_Skus_Detail
    {
        /// <summary>
        /// 价格
        /// </summary>
        public decimal Price { get; set; }
        /// <summary>
        /// 属性名称
        /// </summary>
        public string Properties_name { get; set; } = string.Empty;
        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity { get; set; }
        /// <summary>
        /// SKU-ID
        /// </summary>
        public string Sku_id { get; set; } = string.Empty;
    }

    /// <summary>
    /// 微店产品原始详情卖家信息类
    /// </summary>
    public class ThirdApiWeidianProductOriginObj_Sellerinfo
    {
        /// <summary>
        /// 店铺名称
        /// </summary>
        public string Shop_name { get; set; } = string.Empty;
    }

    /// <summary>
    /// 微店产品原始详情属性图片类
    /// </summary>
    public class ThirdApiWeidianProductOriginObj_Propimgs
    {
        /// <summary>
        /// 属性图列表
        /// </summary>
        public List<ThirdApiWeidianProductOriginObj_Propimgs_Detail> Prop_img { get; set; } = new List<ThirdApiWeidianProductOriginObj_Propimgs_Detail>();
    }

    /// <summary>
    /// 微店产品原始详情属性图片详情类
    /// </summary>
    public class ThirdApiWeidianProductOriginObj_Propimgs_Detail
    {
        /// <summary>
        /// 属性
        /// </summary>
        public string Properties { get; set; } = string.Empty;
        /// <summary>
        /// 图片地址
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// 微店产品原始详情图片类
    /// </summary>
    public class ThirdApiWeidianProductOriginObj_Image
    {
        /// <summary>
        /// 图片地址
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }
}
