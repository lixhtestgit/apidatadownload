using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
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
                decimal productPrice = TypeParseHelper.StrToDecimal(item.Wt_ProductPrice);
                string productTitle = null;
                string productOriginData = null;
                if (productIdResult.Item1 == EMallPlatform.淘宝)
                {
                    if (!string.IsNullOrWhiteSpace(item.Wt_OriginProductDataJson))
                    {
                        //从Excel获取
                        JObject proJObj = JObject.Parse(item.Wt_OriginProductDataJson);
                        productTitle = proJObj.SelectToken("item.title")?.ToString();
                        if (productPrice <= 0)
                        {
                            productPrice = proJObj.SelectToken("item.price")?.ToObject<decimal>() ?? 99999;
                        }
                        productOriginData = item.Wt_OriginProductDataJson;
                    }
                    else
                    {
                        //走万邦-淘宝API获取
                        string requestUrl = detailTaobaoApiUrl.Replace("{productID}", productID);
                        var getResult = await this.PayHttpClient.Get(requestUrl);
                        productOriginData = getResult.Item2;
                        JObject proJObj = JObject.Parse(productOriginData);
                        productTitle = proJObj.SelectToken("item.title")?.ToString();
                        if (productPrice <= 0)
                        {
                            productPrice = proJObj.SelectToken("item.price")?.ToObject<decimal>() ?? 99999;
                        }
                        productOriginData = JsonHelper.ConvertJsonToStr(proJObj);
                    }
                }

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
                                ( Wt_Price ,
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
                        VALUES  ( {productPrice} , -- Wt_Price - decimal
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

            string taobaoOriginJson = @"{""seller"":{""creditLevel"":""17"",""creditLevelIcon"":""//gw.alicdn.com/tfs/TB1MpufDhjaK1RjSZKzXXXVwXXa-165-45.png"",""encryptUid"":""RAzN8HWNgDpTJX75KSyYFuAQSBw4kCebDyyLTeCQmSiiyE7rqM7"",""evaluates"":[{""level"":""1"",""levelText"":""高"",""score"":""4.8 "",""title"":""宝贝描述"",""type"":""desc""},{""level"":""1"",""levelText"":""高"",""score"":""4.8 "",""title"":""卖家服务"",""type"":""serv""},{""level"":""-1"",""levelText"":""低"",""score"":""4.8 "",""title"":""跨境物流"",""type"":""post""}],""pcShopUrl"":""//shop479456214.taobao.com"",""sellerId"":""4066234693"",""sellerNick"":""天猫国际海外直购"",""sellerType"":""B"",""shopIcon"":""https://img.alicdn.com/imgextra/i4/6000000003216/O1CN01iQgDcv1ZcyWMeVMRD_!!6000000003216-2-shopmanager.png"",""shopId"":""479456214"",""shopName"":""天猫国际全球探物"",""tagIcon"":""//gw.alicdn.com/tfs/TB1889mggMPMeJjy1XbXXcwxVXa-113-28.png"",""userId"":""4066234693""},""item"":{""bottomIcons"":[],""images"":[""https://img.alicdn.com/imgextra/i2/4066234693/O1CN01gGmQc81kXRqCZITYg_!!4066234693-0-tmg_sticker_daily.jpg"",""https://img.alicdn.com/imgextra/i3/2201230376636/O1CN01JqgD4l1ytLNZSbJwI_!!2201230376636.jpg"",""https://img.alicdn.com/imgextra/i3/2201230376636/O1CN01chAvo31ytLNfJxmVS_!!2201230376636.jpg"",""https://img.alicdn.com/imgextra/i1/2201230376636/O1CN01NkTRja1ytLNeGCL82_!!2201230376636.jpg"",""https://img.alicdn.com/imgextra/i4/2201230376636/O1CN01tNyAFb1ytLNf4sa7p_!!2201230376636.jpg""],""itemId"":""974271878334"",""pcADescUrl"":""//market.m.taobao.com/app/detail-project/desc/index.html?id=974271878334&descVersion=7.0&type=1&f=icoss!0974271878334!13233260007&sellerType=B"",""qrCode"":""https://h5.m.taobao.com/awp/core/detail.htm?id=974271878334"",""spuId"":""0"",""title"":""1h可退 【美国直邮】ugg 女士 时尚休闲鞋"",""titleIcon"":""//gw.alicdn.com/tfs/TB1KuplSpXXXXawXpXXXXXXXXXX-135-36.png"",""useWirelessDesc"":""true"",""vagueSellCount"":""0""},""feature"":{""pcResistDetail"":""false"",""tmwOverseasScene"":""false"",""pcIdentityRisk"":""false"",""tmallhkScene"":""true""},""plusViewVO"":{""guessLikeVO"":{""bizCode"":"""",""hit"":""true""},""pluginProtectVO"":{""bizCode"":"""",""hit"":""true""},""industryParamVO"":{""basicParamList"":[{""propertyName"":""品牌"",""valueName"":""UGG""},{""propertyName"":""风格"",""valueName"":""复古风""},{""propertyName"":""鞋类功能"",""valueName"":""耐磨""},{""propertyName"":""是否商场同款"",""valueName"":""是""},{""propertyName"":""适用人群"",""valueName"":""青年""},{""propertyName"":""鞋跟款式"",""valueName"":""厚底""},{""propertyName"":""适用场景"",""valueName"":""户外""},{""propertyName"":""适用性别"",""valueName"":""女""},{""propertyName"":""颜色分类"",""valueName"":""Dark Peony""}],""bizCode"":"""",""hit"":""true""},""headAtmosphereBeltVO"":{""bizCode"":"""",""hit"":""true"",""valid"":""false""},""commentListVO"":{""bizCode"":"""",""ext"":{""countShow"":""\""false\""""},""hit"":""true""},""pcFrontSkuQuantityLimitVO"":{""bizCode"":"""",""hit"":""true""}},""skuCore"":{""sku2info"":{""0"":{""logisticsTime"":""预售，付款后14天内发货"",""moreQuantity"":""false"",""price"":{""priceColorNew"":""#7A7A7A"",""priceMoney"":""157800"",""priceText"":""1578"",""priceTitle"":""优惠前""},""quantity"":""59"",""quantityDisplayValue"":""1"",""quantityText"":""有货"",""subPrice"":{""priceBgColor"":""#FF5000"",""priceColor"":""#FFFFFF"",""priceColorNew"":""#FF5000"",""priceMoney"":""138800"",""priceText"":""1388"",""priceTitle"":""券后"",""priceTitleColor"":""#FFFFFF""}},""5928159757751"":{""logisticsTime"":""预售，付款后14天内发货"",""moreQuantity"":""false"",""price"":{""priceColorNew"":""#FF5000"",""priceMoney"":""157800"",""priceText"":""1578""},""quantity"":""0"",""quantityDisplayValue"":""1"",""quantityErrorMsg"":""超出商品库存限制"",""quantityText"":""无货""},""5928159757750"":{""logisticsTime"":""预售，付款后14天内发货"",""moreQuantity"":""false"",""price"":{""priceColorNew"":""#FF5000"",""priceMoney"":""157800"",""priceText"":""1578""},""quantity"":""0"",""quantityDisplayValue"":""1"",""quantityErrorMsg"":""超出商品库存限制"",""quantityText"":""无货""},""5928159757749"":{""logisticsTime"":""预售，付款后14天内发货"",""moreQuantity"":""false"",""price"":{""priceColorNew"":""#FF5000"",""priceMoney"":""157800"",""priceText"":""1578""},""quantity"":""0"",""quantityDisplayValue"":""1"",""quantityErrorMsg"":""超出商品库存限制"",""quantityText"":""无货""},""5928159757748"":{""logisticsTime"":""预售，付款后14天内发货"",""moreQuantity"":""false"",""price"":{""priceColorNew"":""#FF5000"",""priceMoney"":""157800"",""priceText"":""1578""},""quantity"":""0"",""quantityDisplayValue"":""1"",""quantityErrorMsg"":""超出商品库存限制"",""quantityText"":""无货""},""5928159757747"":{""logisticsTime"":""预售，付款后14天内发货"",""moreQuantity"":""false"",""price"":{""priceColorNew"":""#FF5000"",""priceMoney"":""157800"",""priceText"":""1578""},""quantity"":""0"",""quantityDisplayValue"":""1"",""quantityErrorMsg"":""超出商品库存限制"",""quantityText"":""无货""},""5928159757746"":{""logisticsTime"":""预售，付款后14天内发货"",""moreQuantity"":""false"",""price"":{""priceColorNew"":""#7A7A7A"",""priceMoney"":""157800"",""priceText"":""1578"",""priceTitle"":""优惠前""},""quantity"":""28"",""quantityDisplayValue"":""1"",""quantityText"":""有货"",""subPrice"":{""priceBgColor"":""#FF5000"",""priceColor"":""#FFFFFF"",""priceColorNew"":""#FF5000"",""priceMoney"":""138800"",""priceText"":""1388"",""priceTitle"":""券后"",""priceTitleColor"":""#FFFFFF""}},""5928159757745"":{""logisticsTime"":""预售，付款后14天内发货"",""moreQuantity"":""false"",""price"":{""priceColorNew"":""#7A7A7A"",""priceMoney"":""157800"",""priceText"":""1578"",""priceTitle"":""优惠前""},""quantity"":""20"",""quantityDisplayValue"":""1"",""quantityText"":""有货"",""subPrice"":{""priceBgColor"":""#FF5000"",""priceColor"":""#FFFFFF"",""priceColorNew"":""#FF5000"",""priceMoney"":""138800"",""priceText"":""1388"",""priceTitle"":""券后"",""priceTitleColor"":""#FFFFFF""}},""5928159757744"":{""logisticsTime"":""预售，付款后14天内发货"",""moreQuantity"":""false"",""price"":{""priceColorNew"":""#7A7A7A"",""priceMoney"":""157800"",""priceText"":""1578"",""priceTitle"":""优惠前""},""quantity"":""11"",""quantityDisplayValue"":""1"",""quantityText"":""有货"",""subPrice"":{""priceBgColor"":""#FF5000"",""priceColor"":""#FFFFFF"",""priceColorNew"":""#FF5000"",""priceMoney"":""138800"",""priceText"":""1388"",""priceTitle"":""券后"",""priceTitleColor"":""#FFFFFF""}}},""skuItem"":{""itemStatus"":""0"",""renderSku"":""true"",""unitBuy"":""1""}},""params"":{""aplusParams"":""[]"",""trackParams"":{""detailabtestdetail"":""""}},""skuBase"":{""components"":[],""props"":[{""comboProperty"":""false"",""hasGroupTags"":""false"",""hasImage"":""false"",""name"":""规格"",""packProp"":""false"",""pid"":""-1"",""shouldGroup"":""false"",""values"":[{""comboPropertyValue"":""false"",""name"":""36码 脚长220MM"",""sortOrder"":""1"",""vid"":""-1""},{""comboPropertyValue"":""false"",""name"":""37码 脚长230MM"",""sortOrder"":""2"",""vid"":""-2""},{""comboPropertyValue"":""false"",""name"":""38码 脚长240MM"",""sortOrder"":""3"",""vid"":""-3""},{""comboPropertyValue"":""false"",""name"":""39码 脚长250MM"",""sortOrder"":""4"",""vid"":""-4""},{""comboPropertyValue"":""false"",""name"":""40码 脚长260MM"",""sortOrder"":""5"",""vid"":""-5""},{""comboPropertyValue"":""false"",""name"":""41码 脚长270MM"",""sortOrder"":""6"",""vid"":""-6""},{""comboPropertyValue"":""false"",""name"":""42码 脚长280MM"",""sortOrder"":""7"",""vid"":""-7""},{""comboPropertyValue"":""false"",""name"":""43码 脚长290MM"",""sortOrder"":""8"",""vid"":""-8""}]},{""comboProperty"":""false"",""hasGroupTags"":""false"",""hasImage"":""true"",""name"":""颜色分类"",""nameDesc"":""（1）"",""packProp"":""false"",""pid"":""1627207"",""shouldGroup"":""false"",""values"":[{""comboPropertyValue"":""false"",""image"":""https://gw.alicdn.com/bao/uploaded/i2/2201230376636/O1CN01YEjmEg1ytLNg6a7od_!!2201230376636.jpg"",""name"":""Dark Peony"",""sortOrder"":""0"",""vid"":""1036564308""}]}],""skus"":[{""propPath"":""-1:-1;1627207:1036564308"",""skuId"":""5928159757744""},{""propPath"":""-1:-2;1627207:1036564308"",""skuId"":""5928159757745""},{""propPath"":""-1:-3;1627207:1036564308"",""skuId"":""5928159757746""},{""propPath"":""-1:-4;1627207:1036564308"",""skuId"":""5928159757747""},{""propPath"":""-1:-5;1627207:1036564308"",""skuId"":""5928159757748""},{""propPath"":""-1:-6;1627207:1036564308"",""skuId"":""5928159757749""},{""propPath"":""-1:-7;1627207:1036564308"",""skuId"":""5928159757750""},{""propPath"":""-1:-8;1627207:1036564308"",""skuId"":""5928159757751""}]},""pcTrade"":{""bizDataBuyParams"":{},""buyNowUrl"":""//buy.tmall.hk/order/confirm_order.htm"",""pcBuyParams"":{""virtual"":""false"",""buy_now"":""3599.00"",""auction_type"":""b"",""x-uid"":"""",""title"":""1h可退 【美国直邮】ugg 女士 时尚休闲鞋"",""buyer_from"":""ecity"",""page_from_type"":""main_site_pc"",""detailIsLimit"":""false"",""who_pay_ship"":""卖家承担运费"",""rootCatId"":""50006843"",""auto_post1"":null,""routeToNewPc"":""1"",""auto_post"":""false"",""seller_nickname"":""天猫国际全球探物"",""photo_url"":""i2/4066234693/O1CN01gGmQc81kXRqCZITYg_!!4066234693-0-tmg_sticker_daily.jpg"",""current_price"":""3599.00"",""region"":""美国"",""seller_id"":""c52d0c6969556ae2b2e7b004865a7f14"",""etm"":""""},""pcCartParam"":{""xxc"":""taobaoSearch"",""areaId"":""110116""},""tradeType"":""8""},""componentsVO"":{""headerVO"":{""buttons"":[{""background"":{""alpha"":""1.0"",""disabledAlpha"":""1.0"",""gradientColor"":[""#7A3BE7"",""#B948F1""]},""disabled"":""false"",""events"":[{""fields"":{""url"":""//pages.tmall.com/wow/z/import/tmg-ch-tubes/fJiiCQT5DbMxQXAaQeGc""},""type"":""onClick""}],""subTitle"":{},""title"":{""text"":""搜天猫国际""},""type"":""search_in_tmallhk""},{""background"":{""alpha"":""1.0"",""disabledAlpha"":""1.0"",""gradientColor"":[""#343434""]},""disabled"":""false"",""events"":[{""fields"":{""url"":""//shop479456214.taobao.com/search.htm""},""type"":""onClick""}],""subTitle"":{},""title"":{""text"":""搜本店""},""type"":""search_in_store""}],""logoJumpUrl"":""http://www.tmall.hk"",""mallLogo"":""https://img.alicdn.com/imgextra/i1/O1CN01nToS041IQkWFthCav_!!6000000000888-2-tps-936-106.png"",""searchText"":""搜索宝贝""},""headImageVO"":{""images"":[""https://img.alicdn.com/imgextra/i2/4066234693/O1CN01gGmQc81kXRqCZITYg_!!4066234693-0-tmg_sticker_daily.jpg"",""https://img.alicdn.com/imgextra/i3/2201230376636/O1CN01JqgD4l1ytLNZSbJwI_!!2201230376636.jpg"",""https://img.alicdn.com/imgextra/i3/2201230376636/O1CN01chAvo31ytLNfJxmVS_!!2201230376636.jpg"",""https://img.alicdn.com/imgextra/i1/2201230376636/O1CN01NkTRja1ytLNeGCL82_!!2201230376636.jpg"",""https://img.alicdn.com/imgextra/i4/2201230376636/O1CN01tNyAFb1ytLNf4sa7p_!!2201230376636.jpg""],""videos"":[]},""storeCardVO"":{""buttons"":[{""disabled"":""false"",""image"":{""gifAnimated"":""false"",""imageUrl"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg""},""title"":{""text"":""联系客服""},""type"":""customer_service""},{""disabled"":""false"",""events"":[{""fields"":{""url"":""//shop479456214.taobao.com""},""type"":""openUrl""}],""image"":{""gifAnimated"":""false"",""imageUrl"":""https://img.alicdn.com/imgextra/i4/O1CN01jn67ow1ZhYeiTJlZn_!!6000000003226-55-tps-24-24.svg""},""title"":{""text"":""进入店铺""},""type"":""enter_shop""}],""creditLevel"":""17"",""creditLevelIcon"":""//gw.alicdn.com/tfs/TB1MpufDhjaK1RjSZKzXXXVwXXa-165-45.png"",""evaluates"":[{""score"":""4.6"",""title"":""宝贝质量""},{""score"":""4.9"",""title"":""物流速度""},{""score"":""4.3"",""title"":""服务保障""}],""labelList"":[],""overallScore"":""4.4"",""sellerType"":""B"",""shopIcon"":""https://img.alicdn.com/imgextra/i4/6000000003216/O1CN01iQgDcv1ZcyWMeVMRD_!!6000000003216-2-shopmanager.png"",""shopName"":""天猫国际全球探物"",""shopUrl"":""//shop479456214.taobao.com"",""starNum"":""3.5""},""titleVO"":{""salesDesc"":""已售 0"",""subTitles"":[],""title"":{""title"":""1h可退 【美国直邮】ugg 女士 时尚休闲鞋""}},""bannerVO"":{""imageUrl"":""https://img.alicdn.com/imgextra/i2/O1CN01FFzZRW1U7lUORwap8_!!6000000002471-2-tps-1980-160.png"",""type"":""tmgPostGate""},""debugVO"":{""host"":""taodetail033008134080.center.na620@33.8.134.80"",""traceId"":""2150482317639515281767732e17bb""},""umpPriceLogVO"":{""bcType"":""b"",""bs"":""businessScenario"",""dumpInvoke"":""0"",""map"":""{5928159757744:{\""channelKeyD\"":\""empty\"",\""fpChannelKeyD\"":\""empty\"",\""price1\"":\""1578.00\"",\""price2\"":\""1388.00\"",\""price3\"":\""3599.00\"",\""sourceTypeKeyD\"":\""4_taobaoSearch\"",\""utcDNow\"":\""20_19000^24_202100\"",\""utcDPre\"":\""noProm\""}}"",""priceTId"":""2147807d17639496192335623e171f"",""sellerId"":""4066234693"",""sid"":""5928159757744"",""traceId"":""2150482317639515281767732e17bb"",""type"":""99"",""umpCreateTime"":""2025-11-24 10:32:08"",""version"":""2.1"",""xobjectId"":""974271878334""},""deliveryVO"":{""agingDesc"":""预售，付款后14天内发货"",""agingDescColor"":""#1F1F1F"",""areaId"":""110116"",""deliverToCity"":""北京市"",""deliveryFromAddr"":""美国"",""deliveryToAddr"":""北京市 怀柔区"",""deliveryToDistrict"":""怀柔区"",""freight"":""运费: 28.00""},""o2oVo"":{""enableJzLocalizationProduct"":""false""},""bottomBarVO"":{""buyInMobile"":""false"",""leftButtons"":[{""background"":{""alpha"":""1.0"",""disabledAlpha"":""1.0"",""disabledColor"":[""#9D6FFA"",""#8B24DC""],""gradientColor"":[""#9D6FFA"",""#8B24DC""]},""disabled"":""false"",""title"":{""alpha"":""1.0"",""bold"":""true"",""color"":""#ffffff"",""disabledAlpha"":""0.2"",""disabledColor"":""#33ffffff"",""fontSize"":""16"",""text"":""加入购物车""},""type"":""add_cart""},{""background"":{""alpha"":""1.0"",""disabledAlpha"":""1.0"",""disabledColor"":[""#FB49CE"",""#FF2B6C""],""gradientColor"":[""#FB49CE"",""#FF2B6C""]},""disabled"":""false"",""title"":{""alpha"":""1.0"",""bold"":""true"",""color"":""#ffffff"",""disabledAlpha"":""0.2"",""disabledColor"":""#80ffffff"",""fontSize"":""16"",""text"":""领券购买""},""type"":""buy_now""}],""rightButtons"":[{""disabled"":""false"",""icon"":{""alpha"":""1.0"",""color"":""#666666"",""disabledAlpha"":""1.0"",""disabledColor"":""#dddddd"",""iconFontName"":""뀚"",""size"":""14""},""title"":{""alpha"":""1.0"",""bold"":""false"",""color"":""#666666"",""disabledAlpha"":""1.0"",""disabledColor"":""#666666"",""fontSize"":""14"",""text"":""收藏""},""type"":""collect""}]},""extensionInfoVO"":{""infos"":[{""items"":[{""text"":[""官方立减12%省190元""]}],""title"":""优惠"",""type"":""DAILY_COUPON""},{""items"":[{""icon"":""https://gw.alicdn.com/imgextra/i2/O1CN017ZsqJR1wHCv11V4bP_!!6000000006282-2-tps-269-54.png"",""text"":[""0元入会，抢限量好券""]},{""icon"":""https://img.alicdn.com/imgextra/i4/O1CN01rH1w2t1muuzylT92V_!!6000000005015-2-tps-268-53.png"",""text"":[""进补贴会场，抢真5折好货""]}],""title"":""活动"",""type"":""ACTIVITIES""},{""items"":[{""text"":[""不支持7天无理由退货"",""退货宝"",""正品保障"",""证照公示""]}],""title"":""保障"",""type"":""GUARANTEE""},{""items"":[{""text"":[""UGG""],""title"":""品牌""},{""text"":[""复古风""],""title"":""风格""},{""text"":[""耐磨""],""title"":""鞋类功能""},{""text"":[""是""],""title"":""是否商场同款""},{""text"":[""青年""],""title"":""适用人群""},{""text"":[""厚底""],""title"":""鞋跟款式""},{""text"":[""户外""],""title"":""适用场景""},{""text"":[""女""],""title"":""适用性别""},{""text"":[""Dark Peony""],""title"":""颜色分类""}],""title"":""参数"",""type"":""BASE_PROPS""},{""items"":[{""icon"":""https://gw.alicdn.com/imgextra/i2/O1CN01rGRSdc27ieaMPmbtb_!!6000000007831-2-tps-88-88.png"",""text"":[""此商品不支持7天无理由退货""],""title"":""不支持7天无理由退货""},{""icon"":""https://gw.alicdn.com/imgextra/i3/O1CN01ywvaIw1Vf23dTriiP_!!6000000002679-2-tps-88-88.png"",""text"":[""退货运费险保障：选择上门取件，自动减免首重运费；若选择自寄，参照首重标准补偿，具体以“订单详情-退货宝”为准""],""title"":""退货宝""},{""action"":""查看"",""actionLink"":""https://pages.tmall.com/wow/z/import/tmg-rax-home/tmallimportwupr-index?wh_pid=tmg-website%2F4h5m5nfdx7bdnktxpsy3"",""icon"":""https://gw.alicdn.com/imgextra/i2/O1CN01rGRSdc27ieaMPmbtb_!!6000000007831-2-tps-88-88.png"",""text"":[""100%正品，假一赔十""],""title"":""正品保障""},{""action"":""查看"",""actionLink"":""//www.tmall.hk/wow/z/import/pegasus-no-head/Gr3QWZwPfyEaWkbZQfFx?xid=c52d0c6969556ae2b2e7b004865a7f14"",""icon"":""https://gw.alicdn.com/imgextra/i2/O1CN01rGRSdc27ieaMPmbtb_!!6000000007831-2-tps-88-88.png"",""text"":[""证照公示""],""title"":""证照公示""}],""title"":""保障"",""type"":""GUARANTEE_NEW""}]},""payVO"":{""payConfigList"":[{""text"":""信用卡支付""}]},""rightBarVO"":{""buyerButtons"":[{""disabled"":""false"",""href"":""https://pc.taobao.com?channel=item"",""icon"":""https://gw.alicdn.com/imgextra/i4/O1CN0165n4Cr1CGK2faBVbj_!!6000000000053-1-tps-56-56.gif"",""label"":""桌面版"",""priority"":""201"",""type"":""plugin""},{""disabled"":""false"",""icon"":""https://img.alicdn.com/imgextra/i2/O1CN012pqGiT1gp4XhKkkRs_!!6000000004190-2-tps-96-96.png"",""label"":""联系客服"",""priority"":""200"",""type"":""webww2""},{""disabled"":""false"",""href"":""//cart.taobao.com"",""icon"":""https://img.alicdn.com/imgextra/i4/O1CN01FOK30u1SymJbsQUtk_!!6000000002316-2-tps-96-96.png"",""label"":""购物车"",""priority"":""199"",""type"":""cart2""},{""disabled"":""false"",""href"":""https://h5.m.taobao.com/awp/core/detail.htm?id=974271878334"",""icon"":""https://img.alicdn.com/imgextra/i3/O1CN01CkZbKp27arsx4ktdK_!!6000000007814-2-tps-96-96.png"",""label"":""商品码"",""priority"":""198"",""type"":""qrcode""},{""disabled"":""false"",""icon"":""https://img.alicdn.com/imgextra/i1/O1CN01Go6lqn28DnZ3MlmFE_!!6000000007899-2-tps-96-96.png"",""label"":""复制链接"",""priority"":""196"",""type"":""copyUrl""},{""disabled"":""false"",""icon"":""https://img.alicdn.com/imgextra/i1/O1CN01at70Km26oJu1Kk0vt_!!6000000007708-2-tps-96-96.png"",""priority"":""195"",""type"":""feedback""},{""disabled"":""false"",""priority"":""1"",""type"":""backTop""}],""sellerButtons"":[]},""priceVO"":{""extraPrice"":{""hiddenPrice"":""false"",""priceBgColor"":""#FF5000"",""priceColor"":""#FFFFFF"",""priceColorNew"":""#FF5000"",""priceMoney"":""138800"",""priceText"":""1388"",""priceTitle"":""券后"",""priceTitleColor"":""#FFFFFF"",""priceUnit"":""￥""},""isNewStyle"":""true"",""price"":{""hiddenPrice"":""false"",""priceColor"":""#FF4F00"",""priceColorNew"":""#7A7A7A"",""priceMoney"":""157800"",""priceText"":""1578"",""priceTitle"":""探物专享"",""priceTitleColor"":""#FF4F00"",""priceUnit"":""￥""}},""webfontVO"":{""enableWebfont"":""false""},""tabVO"":{""tabList"":[{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg"",""name"":""comments"",""sort"":""1"",""title"":""用户评价""},{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg"",""name"":""base_drops"",""sort"":""2"",""title"":""参数信息""},{""name"":""factory_qualification"",""sort"":""3"",""title"":""验厂资质""},{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg"",""name"":""desc"",""sort"":""4"",""title"":""图文详情""},{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg"",""name"":""recommends"",""sort"":""5"",""title"":""本店推荐""},{""name"":""guessULike"",""sort"":""7"",""title"":""看了又看""}]},""priceDescVO"":{""descItems"":[{""event"":[{""fields"":{""url"":""https://pages.tmall.com/wow/import/act/detail-taxes?wh_biz=tm&wh_weex=true&data=%7B%22taxDescTittle%22%3A%22%E7%A8%8E%E8%B4%B9%E8%AF%B4%E6%98%8E%22%2C%22taxDesc%22%3A%5B%7B%22%E5%95%86%E5%93%81%E8%BF%9B%E5%8F%A3%E7%A8%8E%22%3A%7B%22%E6%82%A8%E6%89%80%E8%B4%AD%E4%B9%B0%E7%9A%84%E5%95%86%E5%93%81%E5%B7%B2%E5%8C%85%E5%90%AB%E8%B7%A8%E5%A2%83%E7%94%B5%E5%95%86%E8%BF%9B%E5%8F%A3%E7%A8%8E%EF%BC%8C%E4%B8%AA%E5%88%AB%E5%95%86%E5%93%81%E7%A8%8E%E8%B4%B9%E7%94%B1%E5%95%86%E5%AE%B6%E6%89%BF%E6%8B%85%EF%BC%8C%E6%82%A8%E6%97%A0%E9%9C%80%E5%86%8D%E8%A1%8C%E6%94%AF%E4%BB%98%E3%80%82%22%3A%22%22%7D%7D%2C%7B%22%E8%BF%9B%E5%8F%A3%E7%A8%8E%E7%A8%8E%E7%8E%87%22%3A%7B%229.1%25%22%3A%22%E4%B8%AD%E5%9B%BD%E6%B5%B7%E5%85%B3%E8%A7%84%E5%AE%9A%EF%BC%8C%E4%B8%8D%E5%90%8C%E7%B1%BB%E7%9B%AE%E7%9A%84%E5%95%86%E5%93%81%E5%BE%81%E6%94%B6%E7%A8%8E%E7%8E%87%E4%B8%8D%E5%90%8C%EF%BC%8C%E8%AF%A5%E5%95%86%E5%93%81%E7%9A%84%E8%BF%9B%E5%8F%A3%E7%A8%8E%E7%8E%87%E4%B8%BA9.1%25%22%7D%7D%2C%7B%22%E8%BF%9B%E5%8F%A3%E7%A8%8E%E8%AE%A1%E7%AE%97%22%3A%7B%22%E8%BF%9B%E5%8F%A3%E7%A8%8E+%3D+%E5%95%86%E5%93%81%E5%AE%8C%E7%A8%8E%E4%BB%B7%E6%A0%BC%28%E5%8C%85%E6%8B%AC%E8%BF%90%E8%B4%B9%29+*+%E7%A8%8E%E7%8E%87%22%3A%22%28%E5%AE%8C%E7%A8%8E%E4%BB%B7%E6%A0%BC%E7%94%B1%E6%B5%B7%E5%85%B3%E6%9C%80%E7%BB%88%E8%AE%A4%E5%AE%9A%29%22%7D%7D%5D%7D""},""type"":""openFloatDialog""},{""fields"":{""page"":""Page_Detail"",""eventId"":""2101"",""arg1"":""Page_Detail_Button-TaxRate"",""args"":{""spm"":""a2141.7631564.taxRate""}},""type"":""userTrack""}],""isRichText"":""true"",""richText"":[{""style"":{""color"":""#999999""},""text"":""进口税 价格已含税 ""},{""imageUrl"":""https://gw.alicdn.com/tfs/TB1J38IkkL0gK0jSZFxXXXWHVXa-38-38.png?getAvatar=avatar"",""style"":{},""type"":""image""}],""sku2Text"":{""0"":""进口税 价格已含税 "",""5928159757751"":""进口税 价格已含税 "",""5928159757750"":""进口税 价格已含税 "",""5928159757749"":""进口税 价格已含税 "",""5928159757748"":""进口税 价格已含税 "",""5928159757747"":""进口税 价格已含税 "",""5928159757746"":""进口税 价格已含税 "",""5928159757745"":""进口税 价格已含税 "",""5928159757744"":""进口税 价格已含税 ""},""text"":""进口税 价格已含税 ""}]}}}";

            JToken taobaoOriginJObj = JObject.Parse(taobaoOriginJson);

            // 创建DTO对象
            ThirdApiProductDetailDto detail = new ThirdApiProductDetailDto();

            // 解析基本信息
            var itemData = taobaoOriginJObj.SelectToken("item");
            var sellerData = taobaoOriginJObj.SelectToken("seller");
            var skuData = taobaoOriginJObj.SelectToken("skuCore.sku2info").ToObject<JObject>();
            var skuPropMappingList = taobaoOriginJObj.SelectToken("skuBase.skus").ToObject<JArray>();
            var propList = taobaoOriginJObj.SelectToken("skuBase.props").ToObject<JArray>();

            //店铺
            detail.ShopID = $"{sellerData.SelectToken("shopId").ToString()}_{sellerData.SelectToken("sellerId").ToString()}";
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

                    string optionPropValue = optionPropDic[$"{optionID}:{choiceID}"];
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
            string descGetUrl = itemData.SelectToken("pcADescUrl").ToString();
            descGetUrl = descGetUrl.StartsWith("//") ? $"https:{descGetUrl}" : descGetUrl;
            //var getDescResult = await this.PayHttpClient.Get(descGetUrl);
            detail.Information = $@"<iframe src=""{descGetUrl}""></iframe>";
            //产品链接
            detail.ProductUrl = $"https://item.taobao.com/item.htm?id={detail.ItemId}";

            return Ok(detail);
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
