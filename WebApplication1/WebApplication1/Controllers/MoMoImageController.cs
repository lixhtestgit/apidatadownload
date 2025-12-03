using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using WebApplication1.DB.CMS;
using WebApplication1.DB.Kaybuy;
using WebApplication1.DB.Repository;
using WebApplication1.Helper;
using WebApplication1.Model.ExcelModel;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// MoMoImage业务控制器
    /// </summary>
    [Route("api/MoMoImage")]
    [ApiController]
    public class MoMoImageController : ControllerBase
    {
        protected HttpClient PayHttpClient;
        public ExcelHelper ExcelHelper;
        private readonly IWebHostEnvironment WebHostEnvironment;
        public ILogger Logger;
        private readonly Lazy<Wd_ThirdProductListRepository> wd_ThirdProductListRepository;
        private Dictionary<string, byte[]> imgUrlDic = new Dictionary<string, byte[]>(100);

        public MoMoImageController(
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            ILogger<OrderShipController> logger,
            Lazy<Wd_ThirdProductListRepository> wd_ThirdProductListRepository)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.Logger = logger;
            this.wd_ThirdProductListRepository = wd_ThirdProductListRepository;
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// 微店店铺产品数据同步
        /// api/MoMoImage/WeiDianShopProductSync?shopID=1677568739&collName=jersey
        /// </summary>
        /// <returns></returns>
        [Route("WeiDianShopProductSync")]
        [HttpGet]
        public async Task WeiDianShopProductSync(string shopID, string collName)
        {
            string paramTemplate = "{\"shopId\":\"{shopID}\",\"tabId\":0,\"sortOrder\":\"desc\",\"offset\":{offset},\"limit\":20,\"from\":\"h5\",\"showItemTag\":true}";

            int page = 1;
            int pageSize = 20;

            int currentPageSize = 0;

            List<Wd_ThirdProductList> fileDataList = new List<Wd_ThirdProductList>();
            do
            {
                int offset = (page - 1) * pageSize;
                string param = paramTemplate.Replace("{shopID}", shopID).Replace("{offset}", offset.ToString());

                string syncUrl = $"https://thor.weidian.com/decorate/shopDetail.tab.getItemList/1.0?param={HttpUtility.UrlEncode(param)}";

                var getResult = await this.PayHttpClient.Get(syncUrl, new Dictionary<string, string>
                {
                    { "Referer", "https://weidian.com/" }
                });
                JObject jObj = JObject.Parse(getResult.Item2);
                JArray pageList = jObj.SelectToken("result.itemList").ToObject<JArray>();
                currentPageSize = pageList.Count;

                if (currentPageSize > 0)
                {
                    foreach (JObject item in pageList)
                    {
                        fileDataList.Add(new Wd_ThirdProductList
                        {
                            Wt_AddTime = DateTime.Now,
                            Wt_CurrentGuID = Guid.NewGuid().ToString(),
                            Wt_IsDelete = 0,
                            Wt_IsTrue = 1,
                            Wt_OriginCollName = collName,
                            Wt_OriginProductID = item.SelectToken("itemId").ToString(),
                            Wt_OriginProductMall = "weidian",
                            Wt_Title = item.SelectToken("itemName").ToString(),
                            //移除额外参数
                            Wt_Image = item.SelectToken("itemImg").ToString().Split('?')[0],
                            Wt_Price = item.SelectToken("price").ToObject<decimal>()
                        });
                    }
                }
                page++;
            } while (currentPageSize == pageSize);

            await this.wd_ThirdProductListRepository.Value.DeleteCollAsync(collName);
            await this.wd_ThirdProductListRepository.Value.AddListAsync(fileDataList.ToArray());

            Console.WriteLine("1");
        }

        /// <summary>
        /// 下载产品图
        /// api/MoMoImage/ExecImage
        /// </summary>
        /// <returns></returns>
        [Route("ExecImage")]
        [HttpGet]
        public async Task ExecImage()
        {
            //1-设置数据源
            string dataDicPath = @$"E:\公司小项目\产品图片收集\数据源\9月上架品类-5-橄榄球-数据整理.xlsx";
            //2-设置保存目录
            string savePath = @"E:\公司小项目\产品图片收集\数据源\9月上架品类-5-橄榄球";

            List<ExcelImageData_MoMo> fileDataList = this.ExcelHelper.ReadTitleDataList<ExcelImageData_MoMo>(dataDicPath, new ExcelFileDescription());

            foreach (ExcelImageData_MoMo item in fileDataList)
            {
                //if (TypeParseHelper.StrToInt32(item.Index) < 75)
                //{
                //    continue;
                //}
                Console.WriteLine($"正在处理第{item.Index}行图片数据...{item.Remark}");

                string fileParentPath = @$"{savePath}\{item.Index}";

                if (Directory.Exists(fileParentPath) && Directory.GetFiles(fileParentPath).Length > 0)
                {
                    Console.WriteLine($"文件已下载,跳过...{fileParentPath}");
                    continue;
                }

                string itemHost = "https://" + item.ProductUrl.Split("https://")[1].Split("/")[0];
                string[] imgUrls = item.Remark?.Split('|').ToArray() ?? new string[0];
                string filePath = "";
                string fileExtendName = "";
                int filePosition = 1;

                imgUrls = imgUrls.Distinct().ToArray();

                if (imgUrls.Length > 0)
                {
                    if (!Directory.Exists(fileParentPath))
                    {
                        Directory.CreateDirectory(fileParentPath);
                    }
                }

                foreach (string imgUrl in imgUrls)
                {
                    Console.WriteLine($"正在处理第{filePosition}/{imgUrls.Length}个图片...");

                    string currentImgUrl = imgUrl;
                    if (!imgUrl.StartsWith("https"))
                    {
                        currentImgUrl = $"{itemHost}/{imgUrl.TrimStart('/')}";
                    }
                    //移除url参数
                    currentImgUrl = currentImgUrl.Split("?")[0];

                    fileExtendName = currentImgUrl.Split('/').Last().Split('.').Last();
                    if (!"jpg,jpeg,png".Contains(fileExtendName))
                    {
                        fileExtendName = "jpg";
                    }

                    filePath = @$"{fileParentPath}\{filePosition}.{fileExtendName}";

                    await this.Download(currentImgUrl, filePath);

                    filePosition++;
                }
            }

            Console.WriteLine($"任务结束...");
        }

        /// <summary>
        /// 根据表格生成插入三方配置产品SQL
        /// api/MoMoImage/CollExcelProduct
        /// </summary>
        /// <returns></returns>
        [Route("CollExcelProduct")]
        [HttpGet]
        public async Task<IActionResult> CollExcelProduct()
        {
            await Task.CompletedTask;
            //1-设置数据源
            string dataDicPath = @$"{this.WebHostEnvironment.ContentRootPath}\示例测试目录\莫莫\专题-淘宝数据收集.xlsx";

            List<ExcelThirdProductData_MoMo> fileDataList = this.ExcelHelper.ReadTitleDataList<ExcelThirdProductData_MoMo>(dataDicPath, new ExcelFileDescription());

            List<string> sqlList = new List<string>();

            //万邦-淘宝API
            string detailTaobaoApiUrl = "https://api-gw.onebound.cn/taobao/item_get?key=t3169987115&secret=7115cf6e&api_name=item_get&result_type=json&num_iid={productID}";

            int index = 0;
            foreach (ExcelThirdProductData_MoMo item in fileDataList)
            {
                index++;

                Console.WriteLine($"正在处理第{index}/{fileDataList.Count}条数据...");

                //获取产品地址分类
                var productIdResult = this.GetProductIdByUrl(item.Wt_ProductUrl);
                if (productIdResult.Item1 == null || string.IsNullOrWhiteSpace(productIdResult.Item2))
                {
                    continue;
                }
                string productID = productIdResult.Item2;

                //获取产品原始数据
                ThirdApiProductDetailDto productDetailDto = null;
                if (productIdResult.Item1 == EMallPlatform.淘宝)
                {
                    if (!string.IsNullOrWhiteSpace(item.Wt_OriginProductDataJson))
                    {
                        //从Excel获取
                        productDetailDto = await this.convertTaobaoJsonToProduct(item.Wt_OriginProductDataJson);
                    }
                    else
                    {
                        //走万邦-淘宝API获取
                        string requestUrl = detailTaobaoApiUrl.Replace("{productID}", productID);
                        var getResult = await this.PayHttpClient.Get(requestUrl);
                        ThirdApiTaobaoProductOriginObj taobaoProductOriginObj = JsonHelper.ConvertStrToJson<ThirdApiTaobaoProductOriginObj>(getResult.Item2);

                        productDetailDto = taobaoProductOriginObj.ConvertThirdApiDetail();
                    }
                }
                string productTitle = item.Wt_ProductTitle;
                if (string.IsNullOrWhiteSpace(productTitle))
                {
                    productTitle = productDetailDto.Name;
                }
                decimal productPrice = TypeParseHelper.StrToDecimal(item.Wt_ProductPrice);
                if (productPrice <= 0)
                {
                    productPrice = productDetailDto.ApplicablePrice;
                }
                string productFirstImage = item.Wt_ProductImage;
                if (string.IsNullOrWhiteSpace(productFirstImage))
                {
                    productFirstImage = productDetailDto.PicUrls.FirstOrDefault() ?? "";
                }
                string productOriginData = JsonHelper.ConvertJsonToStr(productDetailDto);

                if (string.IsNullOrWhiteSpace(productTitle))
                {
                    continue;
                }

                //生成插入脚本
                string productPlatformName = "taobao";
                if (productIdResult.Item1 == EMallPlatform.官方1688)
                {
                    productPlatformName = "1688";
                }
                else if (productIdResult.Item1 == EMallPlatform.微店)
                {
                    productPlatformName = "weidian";
                }

                string insertProductSql = $@"
                        INSERT INTO dbo.Wd_ThirdProductList
                                ( 
                                  Wt_Title,
                                  Wt_Image,
                                  Wt_Price ,
                                  Wt_OriginProductMall ,
                                  Wt_OriginProductID ,
                                  Wt_IsDelete ,
                                  Wt_CurrentGuID ,
                                  Wt_IsTrue ,
                                  Wt_OrderID ,
                                  Wt_AddTime ,
                                  Wt_UpdateTime ,
                                  Wt_OriginProductDataJson ,
                                  Wt_OriginProductUnionKey ,
                                  Wt_IsAutoSync
                                )
                        VALUES  ( N'{productTitle}' , -- Wt_Title
                                  N'{productFirstImage}' , -- Wt_Image                
                                  {productPrice} , -- Wt_Price - decimal
                                  '{productPlatformName}' , -- Wt_OriginProductMall - varchar(10)
                                  '{productID}' , -- Wt_OriginProductID - varchar(100)
                                  0 , -- Wt_IsDelete - bit
                                  N'{Guid.NewGuid().ToString()}' , -- Wt_CurrentGuID - nvarchar(50)
                                  1 , -- Wt_IsTrue - bit
                                  0 , -- Wt_OrderID - int
                                  GETDATE() , -- Wt_AddTime - datetime
                                  GETDATE() , -- Wt_UpdateTime - datetime
                                  N'{productOriginData.Replace("'", "''")}' , -- Wt_OriginProductDataJson - nvarchar(max)
                                  N'{productPlatformName}_{productID}' , -- Wt_OriginProductUnionKey - nvarchar(110)
                                  0  -- Wt_IsAutoSync - bit
                                )
                        ";

                string insertProductCollSql = $@"
                        INSERT INTO dbo.Wd_ThirdProductColl
                                ( Wt_Title ,
                                    Wt_OriginProductMall ,
                                    Wt_OriginProductID ,
                                    Wt_OriginProductUnionKey ,
                                    Wt_ProductTitle,
                                    Wt_ProductImage,
                                    Wt_ProductPrice,
                                    Wt_IsDelete ,
                                    Wt_CurrentGuID ,
                                    Wt_IsTrue ,
                                    Wt_OrderID ,
                                    Wt_AddTime ,
                                    Wt_UpdateTime
                                )
                        VALUES  ( N'{item.Wt_Coll}' , -- Wt_Title - nvarchar(200)
                                    N'{productPlatformName}' , -- Wt_OriginProductMall - nvarchar(10)
                                    N'{productID}' , -- Wt_OriginProductID - nvarchar(100)
                                    N'{productPlatformName}_{productID}' , -- Wt_OriginProductUnionKey - nvarchar(110)
                                    N'{productTitle}' , -- Wt_ProductTitle - nvarchar(200)      
                                    N'{productFirstImage}' , -- Wt_ProductImage - nvarchar(500) 
                                    {productPrice} , -- Wt_ProductPrice - decimal
                                    0 , -- Wt_IsDelete - bit
                                    N'{Guid.NewGuid().ToString()}' , -- Wt_CurrentGuID - nvarchar(50)
                                    1 , -- Wt_IsTrue - bit
                                    0 , -- Wt_OrderID - int
                                    GETDATE() , -- Wt_AddTime - datetime
                                    GETDATE()  -- Wt_UpdateTime - datetime
                                )
                ";

                sqlList.Add(insertProductSql);
                sqlList.Add(insertProductCollSql);
            }

            string sql = string.Join(";", sqlList) + ";";
            return Ok(sql);
        }

        /// <summary>
        /// 转换淘宝Json数据为配置产品数据
        /// api/MoMoImage/ConvertTaobaoJsonToProduct
        /// </summary>
        /// <returns></returns>
        [Route("ConvertTaobaoJsonToProduct")]
        [HttpGet]
        public async Task<IActionResult> ConvertTaobaoJsonToProduct()
        {
            await Task.CompletedTask;

            string taobaoOriginJson = @"{""seller"":{""sellerNick"":""赴家族"",""shopIcon"":""https://img.alicdn.com/imgextra/i2/1731080501/O1CN01s2EiUd1FZVTmcdBzD_!!1731080501.jpg"",""shopName"":""奢店诚品"",""evaluates"":[{""score"":""5.0 "",""level"":""1"",""levelText"":""高"",""title"":""宝贝描述"",""type"":""desc""},{""score"":""5.0 "",""level"":""1"",""levelText"":""高"",""title"":""卖家服务"",""type"":""serv""},{""score"":""5.0 "",""level"":""1"",""levelText"":""高"",""title"":""物流服务"",""type"":""post""}],""userId"":""1731080501"",""creditLevel"":""11"",""sellerId"":""1731080501"",""tagIcon"":""//gtms04.alicdn.com/tps/i4/TB1YE.PHVXXXXb6XXXXSutbFXXX.jpg"",""pcShopUrl"":""//shop261360798.taobao.com"",""creditLevelIcon"":""//gw.alicdn.com/tfs/TB1HfjsiC_I8KJjy0FoXXaFnVXa-132-24.png"",""shopId"":""261360798"",""sellerType"":""C"",""encryptUid"":""RAzN8HWLjRQMqFYGBsg8Z7vAdYV51GZZ7iK5nQv2ebuqs2VhUVo""},""item"":{""vagueSellCount"":""0"",""images"":[""https://img.alicdn.com/imgextra/i3/1731080501/O1CN01rfBHc51FZVWQZrDLA_!!1731080501.jpg"",""https://img.alicdn.com/imgextra/i2/1731080501/O1CN0115W34X1FZVV7oC3Ll_!!1731080501.png"",""https://img.alicdn.com/imgextra/i3/1731080501/O1CN01fBlIFR1FZVV6I4xUs_!!1731080501.png"",""https://img.alicdn.com/imgextra/i3/1731080501/O1CN01vfD6Mv1FZVV6I4l33_!!1731080501.png"",""https://img.alicdn.com/imgextra/i3/1731080501/O1CN01K8a6qq1FZVV5A7rmw_!!1731080501.png""],""title"":""奢店 2025 新款 LV/路易威登 VICTORINE 幻彩短款两折钱包 M25649"",""itemId"":""950800064175"",""useWirelessDesc"":""true"",""qrCode"":""https://h5.m.taobao.com/awp/core/detail.htm?id=950800064175"",""pcADescUrl"":""//market.m.taobao.com/app/detail-project/desc/index.html?id=950800064175&descVersion=7.0&type=1&f=desc/icoss142926708865be296519ba5722&sellerType=C"",""bottomIcons"":[],""spuId"":""0"",""titleIcon"":""""},""feature"":{""pcResistDetail"":""false"",""tmwOverseasScene"":""false"",""pcIdentityRisk"":""false""},""plusViewVO"":{""askAnswerVO"":{""ext"":{""skeletonImg"":""https://img.alicdn.com/imgextra/i2/O1CN01MLPxBr1flZy969k5W_!!6000000004047-2-tps-1376-1216.png""},""spm"":""aliabtest948562_960155"",""hit"":""true"",""bizCode"":""""},""addCartActionVO"":{""ext"":{""type"":""dialogWithRecommond_2"",""frequency"":""day:1;repeat:1""},""spm"":""aliabtest853889_903547"",""hit"":""true"",""bizCode"":""""},""guessLikeVO"":{""hit"":""true"",""bizCode"":""""},""rankVO"":{""spm"":""aliabtest723647_830745"",""hit"":""true"",""bizCode"":""""},""tabPlaceholderVO"":{""spm"":""aliabtest801234_834392"",""hit"":""true"",""bizCode"":""""},""industryParamVO"":{""hit"":""true"",""enhanceParamList"":[{""valueName"":""植物花卉"",""propertyName"":""图案""},{""valueName"":""Louis Vuitton/路易威登"",""propertyName"":""品牌""},{""valueName"":""2025年夏季"",""propertyName"":""上市年份季节""},{""valueName"":""青年"",""propertyName"":""适用对象""},{""valueName"":""牛皮革"",""propertyName"":""质地""},{""valueName"":""车缝线"",""propertyName"":""流行元素""},{""valueName"":""日韩"",""propertyName"":""风格""},{""valueName"":""短款钱包"",""propertyName"":""款式""},{""valueName"":""横款"",""propertyName"":""形状""}],""bizCode"":"""",""basicParamList"":[{""valueName"":""其他"",""propertyName"":""Louis Vuitton/路易威登系列""},{""valueName"":""女"",""propertyName"":""性别""}]},""headAtmosphereBeltVO"":{""eventParam"":{""code"":""dp-PCFenQi-*-online""},""bizCode"":"""",""icon"":""https://img.alicdn.com/imgextra/i3/O1CN01GkQpeo1UyRBbSNgoY_!!6000000002586-2-tps-48-48.png"",""bgColors"":[""#FAEDE1"",""#FAE7D4""],""textColor"":""#11192D"",""valid"":""true"",""actionType"":""timeAction"",""hit"":""true"",""text"":""您有5元红包待使用"",""actionParam"":{""timeActionType"":""countdown"",""leftTime"":""63495"",""timeActionText"":""63495""}},""commentListVO"":{""ext"":{""countShow"":""\""false\""""},""hit"":""true"",""bizCode"":""""},""pcFrontSkuQuantityLimitVO"":{""hit"":""true"",""bizCode"":""""},""buyParamVO"":{""ext"":{""autoApplCoupSource"":""pcDetailOrder"",""needAutoApplCoup"":""true""},""spm"":""aliabtest941180_724531"",""hit"":""true"",""bizCode"":""""}},""skuCore"":{""skuItem"":{""renderSku"":""false"",""itemStatus"":0,""unitBuy"":1},""sku2info"":{""0"":{""moreQuantity"":""false"",""quantity"":1,""logisticsTime"":""预售，30天内发货"",""itemApplyParams"":""[{\""couponName\"":\""满5000减200店铺优惠券\"",\""sellerId\"":1731080501,\""couponType\"":1,\""templateCode\"":\""7821036108\"",\""uuid\"":\""ead341ae7a1f4243bd7929ec623bff0f\""}]"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#7A7A7A"",""priceText"":""6810"",""priceMoney"":""681000""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FF5000"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""6579.68"",""priceMoney"":""657968""}}}},""params"":{""trackParams"":{""detailabtestdetail"":""""},""aplusParams"":""[]""},""skuBase"":{""components"":[],""skus"":[]},""pcTrade"":{""buyNowUrl"":""//buy.taobao.com/auction/buy_now.jhtml"",""bizDataBuyParams"":{},""pcCartParam"":{""areaId"":""110108"",""addressId"":""7689479327""},""pcBuyParams"":{""virtual"":""false"",""buy_now"":""7100.00"",""auction_type"":""b"",""x-uid"":"""",""title"":""奢店 2025 新款 LV/路易威登 VICTORINE 幻彩短款两折钱包 M25649"",""buyer_from"":""ecity"",""page_from_type"":""main_site_pc"",""detailIsLimit"":""false"",""who_pay_ship"":""卖家承担运费"",""rootCatId"":""50006842"",""routeToNewPc"":""1"",""auto_post"":""false"",""seller_nickname"":""奢店诚品"",""photo_url"":""i3/1731080501/O1CN01rfBHc51FZVWQZrDLA_!!1731080501.jpg"",""current_price"":""7100.00"",""region"":""广东深圳"",""seller_id"":""bcbc81f74bfab9a942d1b33d1c9c2150"",""etm"":""""},""tradeType"":1},""componentsVO"":{""headerVO"":{""logoJumpUrl"":""https://www.taobao.com"",""mallLogo"":""https://gw.alicdn.com/imgextra/i1/O1CN01z163bz1lHF5yQ50CC_!!6000000004793-2-tps-172-108.png"",""searchText"":""搜索宝贝"",""buttons"":[{""subTitle"":{},""background"":{""alpha"":""1.0"",""disabledAlpha"":""1.0""},""disabled"":""false"",""title"":{""text"":""搜索""},""type"":""search_in_taobao"",""events"":[{""type"":""onClick"",""fields"":{""url"":""//s.taobao.com/search""}}]},{""subTitle"":{},""background"":{""alpha"":""1.0"",""disabledAlpha"":""1.0""},""disabled"":""false"",""title"":{""text"":""搜本店""},""type"":""search_in_store"",""events"":[{""type"":""onClick"",""fields"":{""url"":""//shop261360798.taobao.com/search.htm""}}]}]},""headImageVO"":{""images"":[""https://img.alicdn.com/imgextra/i3/1731080501/O1CN01rfBHc51FZVWQZrDLA_!!1731080501.jpg"",""https://img.alicdn.com/imgextra/i2/1731080501/O1CN0115W34X1FZVV7oC3Ll_!!1731080501.png"",""https://img.alicdn.com/imgextra/i3/1731080501/O1CN01fBlIFR1FZVV6I4xUs_!!1731080501.png"",""https://img.alicdn.com/imgextra/i3/1731080501/O1CN01vfD6Mv1FZVV6I4l33_!!1731080501.png"",""https://img.alicdn.com/imgextra/i3/1731080501/O1CN01K8a6qq1FZVV5A7rmw_!!1731080501.png""],""videos"":[]},""storeCardVO"":{""buttons"":[{""image"":{""imageUrl"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg"",""gifAnimated"":""false""},""disabled"":""false"",""title"":{""text"":""联系客服""},""type"":""customer_service""},{""image"":{""imageUrl"":""https://img.alicdn.com/imgextra/i4/O1CN01jn67ow1ZhYeiTJlZn_!!6000000003226-55-tps-24-24.svg"",""gifAnimated"":""false""},""disabled"":""false"",""title"":{""text"":""进入店铺""},""type"":""enter_shop"",""events"":[{""type"":""openUrl"",""fields"":{""url"":""//shop261360798.taobao.com""}}]}],""shopIcon"":""https://img.alicdn.com/imgextra/i2/1731080501/O1CN01s2EiUd1FZVTmcdBzD_!!1731080501.jpg"",""shopName"":""奢店诚品"",""evaluates"":[{""score"":""-"",""title"":""宝贝质量""},{""score"":""-"",""title"":""物流速度""},{""score"":""-"",""title"":""服务保障""}],""shopUrl"":""//shop261360798.taobao.com"",""creditLevel"":""11"",""creditLevelIcon"":""//gtms04.alicdn.com/tps/i4/TB1wA25HpXXXXcwXVXXCBGNFFXX-24-24.png"",""sellerType"":""C""},""titleVO"":{""salesDesc"":""已售 0"",""subTitles"":[],""title"":{""title"":""奢店 2025 新款 LV/路易威登 VICTORINE 幻彩短款两折钱包 M25649""}},""debugVO"":{""traceId"":""77f9302017645859147757519e"",""host"":""taodetail011128064174.center.na610@11.128.64.174""},""rateVO"":{""totalCount"":""0"",""favorableRate"":{}},""umpPriceLogVO"":{""umpCreateTime"":""2025-12-01 18:45:15"",""traceId"":""77f9302017645859147757519e"",""xObjectId"":""950800064175"",""type"":99,""bcType"":""c"",""version"":""2.1"",""sId"":""0"",""bS"":""businessScenario"",""sellerId"":""1731080501"",""dUmpInvoke"":0,""map"":""{0:{\""channelKeyD\"":\""empty\"",\""fpChannelKeyD\"":\""empty\"",\""price1\"":\""6810.00\"",\""price2\"":\""6579.68\"",\""price3\"":\""7100.00\"",\""sourceTypeKeyD\"":\""4_null\"",\""utcDNow\"":\""41_29000^13_20000^12_2532^5_500\"",\""utcDPre\"":\""noProm\""}}""},""deliveryVO"":{""agingDescColor"":""#FF5000"",""freight"":""快递: 免运费"",""deliveryFromAddr"":""香港九龙"",""addressId"":""7689479327"",""deliverToCity"":""北京"",""areaId"":110108,""deliveryToAddr"":""北京 海淀 西三旗"",""agingDesc"":""预售，30天内发货"",""deliveryToDistrict"":""海淀""},""o2oVo"":{""enableJzLocalizationProduct"":""false""},""bottomBarVO"":{""rightButtons"":[],""buyInMobile"":""true"",""leftButtons"":[]},""extensionInfoVO"":{""infos"":[{""title"":""优惠"",""type"":""DAILY_COUPON"",""items"":[{""text"":[""红包减5元""]},{""text"":[""淘金币可抵340.50元""]},{""text"":[""店铺券满5000减200""]}]},{""title"":""保障"",""type"":""GUARANTEE"",""items"":[{""text"":[""30天价保"",""不支持7天无理由退货"",""极速退款""]}]},{""title"":""参数"",""type"":""BASE_PROPS"",""items"":[{""text"":[""Louis Vuitton/路易威登""],""title"":""品牌""},{""text"":[""其他""],""title"":""Louis Vuitton/路易威登系列""},{""text"":[""车缝线""],""title"":""流行元素""},{""text"":[""日韩""],""title"":""风格""},{""text"":[""短款钱包""],""title"":""款式""},{""text"":[""2025年夏季""],""title"":""上市年份季节""},{""text"":[""横款""],""title"":""形状""},{""text"":[""牛皮革""],""title"":""质地""},{""text"":[""青年""],""title"":""适用对象""},{""text"":[""植物花卉""],""title"":""图案""},{""text"":[""女""],""title"":""性别""}]},{""title"":""保障"",""type"":""GUARANTEE_NEW"",""items"":[{""icon"":""https://gw.alicdn.com/imgextra/i2/O1CN01KdloOc1iYhaZElYLo_!!6000000004425-2-tps-88-88.png"",""action"":""更多"",""text"":[""付款后30天内降价，可通过“手机淘宝首页搜索-价保中心”申请补差，部分特定场景除外""],""title"":""30天价保"",""actionLink"":""https://rulesale.taobao.com/?type=detail&ruleId=10000095&cId=347#/rule/detail?ruleId=10000095&cId=347""},{""icon"":""https://gw.alicdn.com/imgextra/i2/O1CN01rGRSdc27ieaMPmbtb_!!6000000007831-2-tps-88-88.png"",""text"":[""此商品不支持7天无理由退换""],""title"":""不支持7天无理由退货""},{""icon"":""https://gw.alicdn.com/imgextra/i3/O1CN017M9n9g24KtBtclhMh_!!6000000007373-2-tps-88-88.png"",""text"":[""满足相应条件时，信誉良好的用户在退货寄出后，享受极速退款到账。""],""title"":""极速退款""}]}]},""payVO"":{""payConfigList"":[{""text"":""信用卡支付""}]},""rightBarVO"":{""toolkit"":{""plugin"":{""icon"":""https://gw.alicdn.com/imgextra/i4/O1CN0165n4Cr1CGK2faBVbj_!!6000000000053-1-tps-56-56.gif"",""disabled"":""false"",""href"":""https://pc.taobao.com?channel=item"",""label"":""桌面版"",""priority"":201},""webww2"":{""openType"":""manual"",""icon"":""https://img.alicdn.com/imgextra/i2/O1CN012pqGiT1gp4XhKkkRs_!!6000000004190-2-tps-96-96.png"",""disabled"":""false"",""label"":""联系客服"",""priority"":200},""cart2"":{""icon"":""https://img.alicdn.com/imgextra/i4/O1CN01FOK30u1SymJbsQUtk_!!6000000002316-2-tps-96-96.png"",""disabled"":""false"",""href"":""//cart.taobao.com"",""label"":""购物车"",""priority"":199},""qrcode"":{""priority"":198,""url"":""https://pages-g.m.taobao.com/wow/z/app/detail-next/item/index?fromPc=true&id=950800064175"",""spm"":""0.0.sidebar.qrcode"",""icon"":""https://img.alicdn.com/imgextra/i3/O1CN01CkZbKp27arsx4ktdK_!!6000000007814-2-tps-96-96.png"",""disabled"":""false"",""href"":""https://h5.m.taobao.com/awp/core/detail.htm?id=950800064175"",""label"":""商品码""},""survey"":{""icon"":""https://gw.alicdn.com/imgextra/i3/O1CN01js47DP1J3DxYBQG4g_!!6000000000972-1-tps-56-56.gif"",""disabled"":""false"",""href"":""https://survey.taobao.com/apps/zhiliao/GUShqv-xp"",""label"":""用户调研"",""priority"":197},""copyUrl"":{""openType"":""manual"",""icon"":""https://img.alicdn.com/imgextra/i1/O1CN01Go6lqn28DnZ3MlmFE_!!6000000007899-2-tps-96-96.png"",""disabled"":""false"",""label"":""复制链接"",""priority"":196},""feedback"":{""priority"":195,""icon"":""https://img.alicdn.com/imgextra/i1/O1CN01at70Km26oJu1Kk0vt_!!6000000007708-2-tps-96-96.png"",""disabled"":""false""},""report"":{""priority"":99,""href"":""//jubao.taobao.com/index.htm?itemId=950800064175&spm=a1z6q.7847058"",""icon"":""https://img.alicdn.com/imgextra/i2/O1CN01RAWBfz20zsCKuENux_!!6000000006921-2-tps-96-96.png"",""label"":""举报"",""disabled"":""false""},""backTop"":{""disabled"":""false"",""priority"":1}},""buyerButtons"":[{""icon"":""https://gw.alicdn.com/imgextra/i4/O1CN0165n4Cr1CGK2faBVbj_!!6000000000053-1-tps-56-56.gif"",""disabled"":""false"",""href"":""https://pc.taobao.com?channel=item"",""label"":""桌面版"",""priority"":201,""type"":""plugin""},{""icon"":""https://img.alicdn.com/imgextra/i2/O1CN012pqGiT1gp4XhKkkRs_!!6000000004190-2-tps-96-96.png"",""disabled"":""false"",""label"":""联系客服"",""priority"":200,""type"":""webww2""},{""icon"":""https://img.alicdn.com/imgextra/i4/O1CN01FOK30u1SymJbsQUtk_!!6000000002316-2-tps-96-96.png"",""disabled"":""false"",""href"":""//cart.taobao.com"",""label"":""购物车"",""priority"":199,""type"":""cart2""},{""icon"":""https://img.alicdn.com/imgextra/i3/O1CN01CkZbKp27arsx4ktdK_!!6000000007814-2-tps-96-96.png"",""disabled"":""false"",""href"":""https://h5.m.taobao.com/awp/core/detail.htm?id=950800064175"",""label"":""商品码"",""priority"":198,""type"":""qrcode""},{""icon"":""https://gw.alicdn.com/imgextra/i3/O1CN01js47DP1J3DxYBQG4g_!!6000000000972-1-tps-56-56.gif"",""disabled"":""false"",""href"":""https://survey.taobao.com/apps/zhiliao/GUShqv-xp"",""label"":""用户调研"",""priority"":197,""type"":""survey""},{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN01Go6lqn28DnZ3MlmFE_!!6000000007899-2-tps-96-96.png"",""disabled"":""false"",""label"":""复制链接"",""priority"":196,""type"":""copyUrl""},{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN01at70Km26oJu1Kk0vt_!!6000000007708-2-tps-96-96.png"",""disabled"":""false"",""priority"":195,""type"":""feedback""},{""icon"":""https://img.alicdn.com/imgextra/i2/O1CN01RAWBfz20zsCKuENux_!!6000000006921-2-tps-96-96.png"",""disabled"":""false"",""href"":""//jubao.taobao.com/index.htm?itemId=950800064175&spm=a1z6q.7847058"",""label"":""举报"",""priority"":99,""type"":""report""},{""disabled"":""false"",""priority"":1,""type"":""backTop""}],""sellerButtons"":[]},""priceVO"":{""extraPrice"":{""priceUnit"":""￥"",""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FF5000"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""6579.68"",""priceMoney"":""657968"",""hiddenPrice"":""false""},""price"":{""priceUnit"":""￥"",""priceColor"":""#FF4F00"",""priceTitle"":""夏季热卖"",""priceColorNew"":""#7A7A7A"",""priceTitleColor"":""#FF4F00"",""priceText"":""6810"",""priceMoney"":""681000"",""hiddenPrice"":""false""},""isNewStyle"":""true""},""webfontVO"":{""enableWebfont"":""false""},""tabVO"":{""tabList"":[{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg"",""name"":""comments"",""sort"":1,""title"":""用户评价""},{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg"",""name"":""base_drops"",""sort"":2,""title"":""参数信息""},{""name"":""factory_qualification"",""sort"":3,""title"":""验厂资质""},{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg"",""name"":""desc"",""sort"":4,""title"":""图文详情""},{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg"",""name"":""recommends"",""sort"":5,""title"":""本店推荐""},{""name"":""guessULike"",""sort"":7,""title"":""看了又看""}]}},""frontendVO"":{""skuImageLayoutMode"":""listMode""}}";

            ThirdApiProductDetailDto detail = await this.convertTaobaoJsonToProduct(taobaoOriginJson);

            return Ok(detail);
        }

        private async Task<ThirdApiProductDetailDto> convertTaobaoJsonToProduct(string taobaoOriginDataJson)
        {
            JToken taobaoOriginJObj = JObject.Parse(taobaoOriginDataJson);

            // 创建DTO对象
            ThirdApiProductDetailDto detail = new ThirdApiProductDetailDto();

            // 解析基本信息
            var itemData = taobaoOriginJObj.SelectToken("item");
            var sellerData = taobaoOriginJObj.SelectToken("seller");
            var skuData = taobaoOriginJObj.SelectToken("skuCore.sku2info").ToObject<JObject>();
            var skuPropMappingList = taobaoOriginJObj.SelectToken("skuBase.skus").ToObject<JArray>();
            var propList = taobaoOriginJObj.SelectToken("skuBase.props")?.ToObject<JArray>() ?? new JArray();

            //店铺
            try
            {
                detail.ShopID = $"{sellerData.SelectToken("shopId").ToString()}_{sellerData.SelectToken("sellerId").ToString()}";
            }
            catch (Exception)
            {
                detail.ShopID = "自营";
            }
            detail.ShopName = sellerData.SelectToken("shopName").ToString();
            //产品标识
            detail.ItemId = itemData.SelectToken("itemId").ToString();
            //标题
            detail.Name = itemData.SelectToken("title").ToString();
            //价格
            detail.ApplicablePrice = TypeParseHelper.StrToDecimal(skuData["0"].SelectToken("price.priceText").ToString());

            //主图列表
            JArray imageJArray = itemData.SelectToken("images").ToObject<JArray>();
            if (imageJArray.Any())
            {
                foreach (var image in imageJArray)
                {
                    detail.PicUrls.Add(image.ToString());
                }
                detail.MainImageUrl = detail.PicUrls.FirstOrDefault();
            }

            //属性图列表和属性列表
            detail.ItemOptions = new List<ThirdApiProductDetailDto_ItemOption>();

            Dictionary<string, string> optionPropDic = new Dictionary<string, string>();

            foreach (JToken prop in propList)
            {
                string optionID = prop["pid"].ToString();
                string optionName = prop["name"].ToString();

                ThirdApiProductDetailDto_ItemOption itemOption = detail.ItemOptions.FirstOrDefault(m => m.OptionID == optionID);
                if (itemOption == null)
                {
                    itemOption = new ThirdApiProductDetailDto_ItemOption
                    {
                        OptionID = optionID,
                        Name = optionName,
                        ChoiceList = new List<ThirdApiProductDetailDto_ItemOptionChoice>()
                    };
                    detail.ItemOptions.Add(itemOption);
                }

                JArray propValues = prop["values"].ToObject<JArray>();
                foreach (JToken propValue in propValues)
                {
                    string choiceID = propValue["vid"].ToString();
                    string choiceName = propValue["name"].ToString();

                    string choiceImg = propValue["image"]?.ToString();

                    ThirdApiProductDetailDto_ItemOptionChoice itemOptionChoice = itemOption.ChoiceList.FirstOrDefault(m => m.ChoiceID == choiceID);
                    if (itemOptionChoice == null)
                    {
                        itemOptionChoice = new ThirdApiProductDetailDto_ItemOptionChoice();
                        itemOption.ChoiceList.Add(itemOptionChoice);
                    }

                    itemOptionChoice.ChoiceID = choiceID;
                    itemOptionChoice.Name = choiceName;
                    itemOptionChoice.Src = choiceImg;
                }
            }

            //SKU
            detail.Skus = new List<ThirdApiProductDetailDto_Sku>();

            Dictionary<string, string> skuIDPropMappingDic = new System.Collections.Generic.Dictionary<string, string>();
            foreach (JToken skuPropMapping in skuPropMappingList)
            {
                skuIDPropMappingDic.Add(skuPropMapping["skuId"].ToString(), skuPropMapping["propPath"].ToString());
            }

            foreach (JToken skuJToken in skuPropMappingList)
            {
                string skuID = skuJToken["skuId"].ToString();
                JToken skuItemPropObj = skuData[skuID];
                //SKU库存
                int skuQuantity = skuItemPropObj["quantity"].ToObject<int>();
                decimal skuPrice = skuItemPropObj.SelectToken("price.priceText").ToObject<decimal>();

                ThirdApiProductDetailDto_Sku sku = new ThirdApiProductDetailDto_Sku
                {
                    Price = skuPrice,
                    StockQuantity = skuQuantity,
                    SkuId = skuID,
                    Properties = new List<ThirdApiProductDetailDto_Property>()
                };


                string propMapping = skuJToken["propPath"].ToString();
                string[] propMappingArray = propMapping.Split(';');

                foreach (var item in propMappingArray)
                {
                    string[] itemArray = item.Split(':');
                    string optionID = itemArray[0];
                    string choiceID = itemArray[1];

                    string optionChoiceKey = $"{optionID}:{choiceID}";
                    if (!optionPropDic.ContainsKey(optionChoiceKey))
                    {
                        continue;
                    }

                    string optionPropValue = optionPropDic[optionChoiceKey];
                    string[] optionPropValueArray = optionPropValue.Split(':');

                    string optionName = optionPropValueArray[0];
                    string choiceName = optionPropValueArray[1];

                    sku.Properties.Add(new ThirdApiProductDetailDto_Property
                    {
                        OptionID = optionID,
                        OptionName = optionName,
                        ChoiceID = choiceID,
                        ChoiceName = choiceName
                    });
                }

                detail.Skus.Add(sku);
            }

            //描述
            //string descGetUrl = itemData.SelectToken("pcADescUrl")?.ToString();
            //if (!string.IsNullOrWhiteSpace(descGetUrl))
            //{
            //    descGetUrl = descGetUrl.StartsWith("//") ? $"https:{descGetUrl}" : descGetUrl;
            //    //var getDescResult = await this.PayHttpClient.Get(descGetUrl);
            //    detail.Information = await this.GetWebUrlDomElement(descGetUrl, "#root");
            //}

            //产品链接
            detail.ProductUrl = $"https://item.taobao.com/item.htm?id={detail.ItemId}";

            return detail;
        }

        private async Task Download(string webFileUrl, string filePath)
        {
            //如果图片已下载，直接保存
            if (this.imgUrlDic.ContainsKey(webFileUrl))
            {
                var imageBytes = this.imgUrlDic[webFileUrl];
                System.IO.File.WriteAllBytes(filePath, imageBytes);
                return;
            }

            var client = new HttpClient();
            //client.DefaultRequestVersion = HttpVersion.Version11;
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,zh;q=0.9,en;q=0.8");

            if (webFileUrl.Contains("www.vancleefarpels.com"))
            {
                //设置访问头
                client.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Not;A=Brand\";v=\"99\", \"Google Chrome\";v=\"139\", \"Chromium\";v=\"139\"");
                client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
                client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
                client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            }
            else if (webFileUrl.Contains("m.media-amazon.com"))
            {
                //移除尺寸标记
                webFileUrl = webFileUrl.Replace("38,50_", "");
            }
            else if (webFileUrl.Contains("stadiumgoods.com"))
            {
                //移除尺寸标记
                webFileUrl = webFileUrl.Replace("38,50_", "");
            }
            else if (webFileUrl.Contains("fansidea.com"))
            {
                //移除尺寸标记
                webFileUrl = webFileUrl.Replace("_800x", "");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, webFileUrl);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode)
            {
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                this.imgUrlDic.Add(webFileUrl, imageBytes);
                System.IO.File.WriteAllBytes(filePath, imageBytes);
            }
        }

        /// <summary>
        /// 根据产品原始链接获取产品ID
        /// </summary>
        /// <param name="productUrl"></param>
        /// <returns></returns>
        private (EMallPlatform?, string) GetProductIdByUrl(string productUrl)
        {
            if (string.IsNullOrWhiteSpace(productUrl))
            {
                return (null, "");
            }
            if (!productUrl.StartsWith("https:"))
            {
                return (null, productUrl);
            }

            string? productID = productUrl;
            EMallPlatform mallPlatform = EMallPlatform.其他;

            Uri downloadUrlUri = new Uri(productID);
            var queryString = HttpUtility.ParseQueryString(downloadUrlUri.Query);

            //1688产品
            string itemDownloadUrlNoParam = productID.Split('?')[0];
            if (itemDownloadUrlNoParam.Contains("1688."))
            {
                string downloadUrl = productID.Split('?')[0];
                productID = downloadUrl.Split('/').LastOrDefault()?.Split('.')[0];
                mallPlatform = EMallPlatform.官方1688;
            }
            //淘宝+天猫产品
            else if (itemDownloadUrlNoParam.Contains("taobao.") || itemDownloadUrlNoParam.Contains("tmall."))
            {
                productID = queryString.Get("id");
                mallPlatform = EMallPlatform.淘宝;
            }
            //微店产品
            else if (itemDownloadUrlNoParam.Contains("weidian."))
            {
                productID = queryString.Get("itemID");
                mallPlatform = EMallPlatform.微店;
            }

            return (mallPlatform, productID ?? productUrl);
        }

        /// <summary>
        /// 店铺平台枚举
        /// </summary>
        private enum EMallPlatform
        {
            /// <summary>
            /// 其他
            /// </summary>
            其他 = 0,

            /// <summary>
            /// 1688
            /// </summary>
            官方1688 = 7,
            /// <summary>
            /// 淘宝
            /// </summary>
            淘宝 = 12,
            /// <summary>
            /// 微店
            /// </summary>
            微店 = 14,
        }


    }
}
