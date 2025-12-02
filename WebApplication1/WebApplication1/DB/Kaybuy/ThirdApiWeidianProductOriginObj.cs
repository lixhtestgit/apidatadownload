using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication1.DB.Kaybuy
{
    /// <summary>
    /// 万邦-微店产品原始详情类
    /// </summary>
    public class ThirdApiWeidianProductOriginObj
    {
        /// <summary>
        /// 详情
        /// </summary>
        public ThirdApiWeidianProductOriginObj_Item Item { get; set; } = new ThirdApiWeidianProductOriginObj_Item();

        /// <summary>
        /// 转换成三方产品详情DTO
        /// </summary>
        /// <returns></returns>
        public ThirdApiProductDetailDto ConvertThirdApiDetail()
        {
            //转化为统一产品类返回
            ThirdApiProductDetailDto productDetailDto = new ThirdApiProductDetailDto
            {
                ShopID = $"{this.Item.Shop_id}_{this.Item.Seller_id}",
                ShopName = this.Item.Seller_info?.Shop_name ?? "",
                ItemId = this.Item.Num_iid,
                Name = this.Item.Title,
                ApplicablePrice = this.Item.Price,
                PicUrls = this.Item.Item_imgs?.Select(m => m.Url).ToList() ?? new List<string>(),
                ItemOptions = new List<ThirdApiProductDetailDto_ItemOption>(),
                Skus = new List<ThirdApiProductDetailDto_Sku>(),
                Information = this.Item.Desc,
                ProductUrl = $"https://item.taobao.com/item.htm?id={this.Item.Num_iid}"
            };
            productDetailDto.MainImageUrl = productDetailDto.PicUrls.FirstOrDefault();

            //补充产品选项信息
            JToken propsListJToken = JToken.FromObject(this.Item.Props_list ?? "{}");
            if (propsListJToken.Children().Count() > 0)
            {
                Dictionary<string, string> propsListDic = propsListJToken.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>(0);
                foreach (var item in propsListDic)
                {
                    //item: "0:9604894249": "尺寸:36 eu"

                    if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
                    {
                        continue;
                    }
                    string[] itemKeys = item.Key.Split(':');
                    string[] itemValues = item.Value.Split(':');

                    if (itemKeys.Length < 2 || itemValues.Length < 2)
                    {
                        continue;
                    }

                    string optionID = itemKeys[0];
                    string optionName = itemValues[0];
                    ThirdApiProductDetailDto_ItemOption? itemOption = productDetailDto.ItemOptions.FirstOrDefault(m => m.OptionID == optionID);
                    if (itemOption == null)
                    {
                        itemOption = new ThirdApiProductDetailDto_ItemOption
                        {
                            OptionID = optionID,
                            Name = optionName,
                            ChoiceList = new List<ThirdApiProductDetailDto_ItemOptionChoice>()
                        };
                        productDetailDto.ItemOptions.Add(itemOption);
                    }

                    string choiceID = itemKeys[1];
                    string choiceName = itemValues[1];

                    ThirdApiProductDetailDto_ItemOptionChoice? itemOptionChoice = itemOption.ChoiceList.FirstOrDefault(m => m.ChoiceID == choiceID);
                    if (itemOptionChoice == null)
                    {
                        itemOptionChoice = new ThirdApiProductDetailDto_ItemOptionChoice
                        {
                            ChoiceID = choiceID,
                            Name = choiceName,
                            Src = this.Item.Prop_imgs.Prop_img.FirstOrDefault(m => m.Properties == $"{optionID}:{choiceID}")?.Url ?? ""
                        };
                        itemOption.ChoiceList.Add(itemOptionChoice);
                    }
                }
            }

            //补充产品选项库存和价格
            if (this.Item.Skus != null && this.Item.Skus.Sku != null && this.Item.Skus.Sku.Count > 0)
            {
                foreach (ThirdApiWeidianProductOriginObj_Skus_Detail skuItem in this.Item.Skus.Sku)
                {
                    ThirdApiProductDetailDto_Sku optionStockPrice = new ThirdApiProductDetailDto_Sku
                    {
                        SkuId = skuItem.Sku_id,
                        Price = skuItem.Price,
                        StockQuantity = skuItem.Quantity,
                        Properties = new List<ThirdApiProductDetailDto_Property>()
                    };
                    productDetailDto.Skus.Add(optionStockPrice);

                    string[] itemProps = skuItem.Properties_name.Split(';');
                    foreach (string itemProp in itemProps)
                    {
                        //itemProp1: 0:9604894249:尺寸:36 eu
                        //itemProp2: 0:9604894249:尺寸:36 eu;

                        if (string.IsNullOrWhiteSpace(itemProp))
                        {
                            continue;
                        }
                        string[] itemPropGroupArray = itemProp.Split(':');
                        if (itemPropGroupArray.Length < 4)
                        {
                            continue;
                        }

                        optionStockPrice.Properties.Add(new ThirdApiProductDetailDto_Property
                        {
                            OptionID = itemPropGroupArray[0],
                            ChoiceID = itemPropGroupArray[1],
                            OptionName = itemPropGroupArray[2],
                            ChoiceName = itemPropGroupArray[3]
                        });
                    }
                }
            }
            else
            {
                ThirdApiProductDetailDto_Sku optionStockPrice = new ThirdApiProductDetailDto_Sku
                {
                    SkuId = this.Item.Num_iid,
                    Price = this.Item.Price,
                    StockQuantity = 99999,
                    Properties = new List<ThirdApiProductDetailDto_Property>()
                };
                productDetailDto.Skus.Add(optionStockPrice);
            }

            return productDetailDto;
        }
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
        public ThirdApiWeidianProductOriginObj_Sellerinfo? Seller_info { get; set; }
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
        /// <summary>
        /// 店铺唯一ID
        /// </summary>
        public string User_num_id { get; set; } = string.Empty;
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
