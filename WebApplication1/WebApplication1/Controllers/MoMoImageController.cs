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
            string dataDicPath = @$"C:\Users\lixianghong\Desktop\aaa.xlsx";

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

            string taobaoOriginJson = @"{""appData"":null,""loaderData"":{""home"":{""data"":{""res"":{""seller"":{""sellerNick"":""lauooooo"",""shopIcon"":""//img.alicdn.com/imgextra//65/73/TB1iVQyciMnBKNjSZFzSuw_qVXa.jpg"",""shopName"":""胖胖小丸子海外购"",""evaluates"":[{""score"":""4.8 "",""level"":""1"",""levelText"":""高"",""title"":""宝贝描述"",""type"":""desc""},{""score"":""4.8 "",""level"":""1"",""levelText"":""高"",""title"":""卖家服务"",""type"":""serv""},{""score"":""4.8 "",""level"":""0"",""levelText"":""平"",""title"":""物流服务"",""type"":""post""}],""userId"":""1013510066"",""creditLevel"":""10"",""sellerId"":""1013510066"",""pcShopUrl"":""//shop297488024.taobao.com"",""tagIcon"":""//gw.alicdn.com/tfscom/TB1rF4lLXXXXXcOaXXXXXXXXXXX.png"",""creditLevelIcon"":""//gw.alicdn.com/tfs/TB1wyMhisLJ8KJjy0FnXXcFDpXa-132-24.png"",""shopId"":""297488024"",""sellerType"":""C"",""encryptUid"":""RAzN8HWRuv7eUL27XhQi6znZ1iaevrZtVsJKsDZLsFjzygyWf8w""},""item"":{""vagueSellCount"":""10"",""images"":[""https://img.alicdn.com/imgextra/i1/1013510066/O1CN01PNCWJf1CMHEmiWY0k_!!1013510066.jpg"",""https://img.alicdn.com/imgextra/i2/1013510066/O1CN01YCFjsx1CMHEoa60mU~crop,0,185,1179,1179~_!!1013510066.jpg"",""https://img.alicdn.com/imgextra/i2/1013510066/O1CN01Mjdk221CMHEmrtl9v~crop,0,183,1179,1179~_!!1013510066.jpg"",""https://img.alicdn.com/imgextra/i4/1013510066/O1CN01xEQALV1CMHEiZPBSA~crop,0,187,1179,1179~_!!1013510066.jpg"",""https://img.alicdn.com/imgextra/i3/1013510066/O1CN012pQTb21CMHEpo7CIP_!!1013510066.jpg""],""title"":""UGG2025秋冬设计师款蒂茨库特 长毛皮毛一体拖鞋1173930"",""itemId"":""985061667610"",""useWirelessDesc"":""true"",""qrCode"":""https://h5.m.taobao.com/awp/core/detail.htm?id=985061667610"",""pcADescUrl"":""//market.m.taobao.com/app/detail-project/desc/index.html?id=985061667610&descVersion=7.0&type=1&f=desc/icoss2294285070d3d2cb9d30ccbde2&sellerType=C"",""bottomIcons"":[],""spuId"":""0"",""titleIcon"":""https://img.alicdn.com/tfs/TB12Rdd1.z1gK0jSZLeXXb9kVXa-104-32.png""},""feature"":{""pcResistDetail"":""false"",""tmwOverseasScene"":""false"",""pcIdentityRisk"":""false""},""plusViewVO"":{""askAnswerVO"":{""ext"":{""skeletonImg"":""https://img.alicdn.com/imgextra/i2/O1CN01MLPxBr1flZy969k5W_!!6000000004047-2-tps-1376-1216.png""},""spm"":""aliabtest941813_960155"",""hit"":""true"",""bizCode"":""""},""addCartActionVO"":{""ext"":{""type"":""dialogWithRecommond_2"",""frequency"":""day:1;repeat:1""},""spm"":""aliabtest853889_903547"",""hit"":""true"",""bizCode"":""""},""guessLikeVO"":{""hit"":""true"",""bizCode"":""""},""rankVO"":{""spm"":""aliabtest723647_830745"",""hit"":""true"",""bizCode"":""""},""tabPlaceholderVO"":{""spm"":""aliabtest801234_834392"",""hit"":""true"",""bizCode"":""""},""industryParamVO"":{""hit"":""true"",""enhanceParamList"":[{""valueName"":""平跟"",""propertyName"":""鞋跟款式""},{""valueName"":""Disquette Chalet"",""propertyName"":""货号""},{""valueName"":""羊毛"",""propertyName"":""鞋垫材质""},{""valueName"":""UGG"",""propertyName"":""品牌""},{""valueName"":""纯羊毛"",""propertyName"":""内里材质""},{""valueName"":""EVA"",""propertyName"":""鞋底材质""},{""valueName"":""日常"",""propertyName"":""适用场景""},{""valueName"":""2025年秋季"",""propertyName"":""上市年份季节""},{""valueName"":""便捷"",""propertyName"":""功能""}],""bizCode"":"""",""basicParamList"":[{""valueName"":""欧美风"",""propertyName"":""风格""},{""valueName"":""5=欧码36=22cm,6=欧码37=23cm,7=欧码38=24cm,8=欧码39=25cm,9=欧码40=26cm,10=欧码41=27cm,11=欧码42=28cm,12=欧码43=29cm"",""propertyName"":""尺码""},{""valueName"":""是"",""propertyName"":""是否商场同款""},{""valueName"":""包头"",""propertyName"":""鞋头款式""},{""valueName"":""少女,中年"",""propertyName"":""适用人群""},{""valueName"":""女"",""propertyName"":""适用性别""},{""valueName"":""一脚蹬"",""propertyName"":""闭合方式""},{""valueName"":""中"",""propertyName"":""防滑性能""},{""valueName"":""蒂茨库特"",""propertyName"":""系列""},{""valueName"":""秋冬"",""propertyName"":""适用季节""},{""valueName"":""否"",""propertyName"":""是否包跟""},{""valueName"":""蓝色/Reef Blue 美国发货,沙堡棕色/Sandcastle 美国发乎,黑色/Black 美国发货,美国直邮，假一赔十"",""propertyName"":""颜色分类""}]},""headAtmosphereBeltVO"":{""eventParam"":{""code"":""dp-PCFenQi-*-online""},""bizCode"":"""",""icon"":""https://img.alicdn.com/imgextra/i3/O1CN01GkQpeo1UyRBbSNgoY_!!6000000002586-2-tps-48-48.png"",""textColor"":""#11192D"",""bgColors"":[""#FAEDE1"",""#FAE7D4""],""valid"":""true"",""actionType"":""timeAction"",""hit"":""true"",""text"":""您有3元红包待使用"",""actionParam"":{""timeActionType"":""countdown"",""leftTime"":""20622"",""timeActionText"":""20622""}},""commentListVO"":{""ext"":{""countShow"":""\""false\""""},""hit"":""true"",""bizCode"":""""},""pcFrontSkuQuantityLimitVO"":{""hit"":""true"",""bizCode"":""""},""buyParamVO"":{""ext"":{""autoApplCoupSource"":""pcDetailOrder"",""needAutoApplCoup"":""true""},""spm"":""aliabtest941180_724531"",""hit"":""true"",""bizCode"":""""}},""skuCore"":{""skuItem"":{""renderSku"":""true"",""itemStatus"":0,""unitBuy"":1},""sku2info"":{""0"":{""moreQuantity"":""true"",""quantity"":200,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""quantityDisplayValue"":1,""quantityText"":""有货"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162853"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162852"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162855"":{""moreQuantity"":""false"",""quantity"":5,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162854"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162849"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162848"":{""moreQuantity"":""false"",""quantity"":6,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162851"":{""moreQuantity"":""false"",""quantity"":3,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162850"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162860"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162857"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162856"":{""moreQuantity"":""false"",""quantity"":6,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162859"":{""moreQuantity"":""false"",""quantity"":6,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162858"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162829"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162831"":{""moreQuantity"":""false"",""quantity"":6,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162830"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162837"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162836"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162839"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162838"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162833"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162832"":{""moreQuantity"":""false"",""quantity"":6,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162835"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162834"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162845"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162844"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162847"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162846"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162841"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162840"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162843"":{""moreQuantity"":""false"",""quantity"":6,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}},""6115344162842"":{""moreQuantity"":""false"",""quantity"":7,""logisticsTime"":""预售，7天内发货"",""price"":{""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceText"":""1490"",""priceMoney"":""149000""},""cartParam"":{""addCartCheck"":""true""},""quantityDisplayValue"":1,""quantityText"":""即将售罄"",""subPrice"":{""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800""}}}},""params"":{""trackParams"":{""detailabtestdetail"":""""},""aplusParams"":""[]""},""skuBase"":{""components"":[],""skus"":[{""propPath"":""1627207:40976357771;20549:9181973233"",""skuId"":""6115344162851""},{""propPath"":""1627207:40976357771;20549:9181973234"",""skuId"":""6115344162855""},{""propPath"":""1627207:40976357771;20549:9181973235"",""skuId"":""6115344162859""},{""propPath"":""1627207:40976357771;20549:14360986509"",""skuId"":""6115344162831""},{""propPath"":""1627207:40976357771;20549:9111157657"",""skuId"":""6115344162847""},{""propPath"":""1627207:40976357771;20549:7913434040"",""skuId"":""6115344162835""},{""propPath"":""1627207:40976357771;20549:7913434041"",""skuId"":""6115344162839""},{""propPath"":""1627207:40976357771;20549:7913434042"",""skuId"":""6115344162843""},{""propPath"":""1627207:40976357772;20549:9181973233"",""skuId"":""6115344162852""},{""propPath"":""1627207:40976357772;20549:9181973234"",""skuId"":""6115344162856""},{""propPath"":""1627207:40976357772;20549:9181973235"",""skuId"":""6115344162860""},{""propPath"":""1627207:40976357772;20549:14360986509"",""skuId"":""6115344162832""},{""propPath"":""1627207:40976357772;20549:9111157657"",""skuId"":""6115344162848""},{""propPath"":""1627207:40976357772;20549:7913434040"",""skuId"":""6115344162836""},{""propPath"":""1627207:40976357772;20549:7913434041"",""skuId"":""6115344162840""},{""propPath"":""1627207:40976357772;20549:7913434042"",""skuId"":""6115344162844""},{""propPath"":""1627207:25969677697;20549:9181973233"",""skuId"":""6115344162849""},{""propPath"":""1627207:25969677697;20549:9181973234"",""skuId"":""6115344162853""},{""propPath"":""1627207:25969677697;20549:9181973235"",""skuId"":""6115344162857""},{""propPath"":""1627207:25969677697;20549:14360986509"",""skuId"":""6115344162829""},{""propPath"":""1627207:25969677697;20549:9111157657"",""skuId"":""6115344162845""},{""propPath"":""1627207:25969677697;20549:7913434040"",""skuId"":""6115344162833""},{""propPath"":""1627207:25969677697;20549:7913434041"",""skuId"":""6115344162837""},{""propPath"":""1627207:25969677697;20549:7913434042"",""skuId"":""6115344162841""},{""propPath"":""1627207:31073814112;20549:9181973233"",""skuId"":""6115344162850""},{""propPath"":""1627207:31073814112;20549:9181973234"",""skuId"":""6115344162854""},{""propPath"":""1627207:31073814112;20549:9181973235"",""skuId"":""6115344162858""},{""propPath"":""1627207:31073814112;20549:14360986509"",""skuId"":""6115344162830""},{""propPath"":""1627207:31073814112;20549:9111157657"",""skuId"":""6115344162846""},{""propPath"":""1627207:31073814112;20549:7913434040"",""skuId"":""6115344162834""},{""propPath"":""1627207:31073814112;20549:7913434041"",""skuId"":""6115344162838""},{""propPath"":""1627207:31073814112;20549:7913434042"",""skuId"":""6115344162842""}],""props"":[{""hasGroupTags"":""false"",""nameDesc"":""（4）"",""packProp"":""false"",""shouldGroup"":""false"",""comboProperty"":""false"",""values"":[{""comboPropertyValue"":""false"",""vid"":""40976357771"",""image"":""https://gw.alicdn.com/bao/uploaded/i1/1013510066/O1CN01NdDbM31CMHEoa7Lvr_!!1013510066.jpg"",""sortOrder"":0,""name"":""蓝色/Reef Blue 美国发货""},{""comboPropertyValue"":""false"",""vid"":""40976357772"",""image"":""https://gw.alicdn.com/bao/uploaded/i4/1013510066/O1CN01E4GypR1CMHEoxn91T_!!1013510066.jpg"",""sortOrder"":9,""name"":""沙堡棕色/Sandcastle 美国发乎""},{""comboPropertyValue"":""false"",""vid"":""25969677697"",""image"":""https://gw.alicdn.com/bao/uploaded/i1/1013510066/O1CN01lEUgTZ1CMHEoa7UFv_!!1013510066.jpg"",""sortOrder"":10,""name"":""黑色/Black 美国发货""},{""comboPropertyValue"":""false"",""vid"":""31073814112"",""image"":""https://gw.alicdn.com/bao/uploaded/i1/1013510066/O1CN01hv35E71CMH8nCWroh_!!1013510066.jpg"",""sortOrder"":11,""name"":""美国直邮，假一赔十""}],""name"":""颜色分类"",""hasImage"":""true"",""pid"":""1627207"",""valueMap"":{""40976357771"":{""comboPropertyValue"":""false"",""vid"":""40976357771"",""image"":""https://gw.alicdn.com/bao/uploaded/i1/1013510066/O1CN01NdDbM31CMHEoa7Lvr_!!1013510066.jpg"",""sortOrder"":0,""name"":""蓝色/Reef Blue 美国发货""},""40976357772"":{""comboPropertyValue"":""false"",""vid"":""40976357772"",""image"":""https://gw.alicdn.com/bao/uploaded/i4/1013510066/O1CN01E4GypR1CMHEoxn91T_!!1013510066.jpg"",""sortOrder"":9,""name"":""沙堡棕色/Sandcastle 美国发乎""},""25969677697"":{""comboPropertyValue"":""false"",""vid"":""25969677697"",""image"":""https://gw.alicdn.com/bao/uploaded/i1/1013510066/O1CN01lEUgTZ1CMHEoa7UFv_!!1013510066.jpg"",""sortOrder"":10,""name"":""黑色/Black 美国发货""},""31073814112"":{""comboPropertyValue"":""false"",""vid"":""31073814112"",""image"":""https://gw.alicdn.com/bao/uploaded/i1/1013510066/O1CN01hv35E71CMH8nCWroh_!!1013510066.jpg"",""sortOrder"":11,""name"":""美国直邮，假一赔十""}}},{""hasGroupTags"":""false"",""packProp"":""false"",""shouldGroup"":""false"",""comboProperty"":""false"",""values"":[{""comboPropertyValue"":""false"",""vid"":""9181973233"",""sortOrder"":1,""name"":""5=欧码36=22cm""},{""comboPropertyValue"":""false"",""vid"":""9181973234"",""sortOrder"":2,""name"":""6=欧码37=23cm""},{""comboPropertyValue"":""false"",""vid"":""9181973235"",""sortOrder"":3,""name"":""7=欧码38=24cm""},{""comboPropertyValue"":""false"",""vid"":""14360986509"",""sortOrder"":4,""name"":""8=欧码39=25cm""},{""comboPropertyValue"":""false"",""vid"":""9111157657"",""sortOrder"":5,""name"":""9=欧码40=26cm""},{""comboPropertyValue"":""false"",""vid"":""7913434040"",""sortOrder"":6,""name"":""10=欧码41=27cm""},{""comboPropertyValue"":""false"",""vid"":""7913434041"",""sortOrder"":7,""name"":""11=欧码42=28cm""},{""comboPropertyValue"":""false"",""vid"":""7913434042"",""sortOrder"":8,""name"":""12=欧码43=29cm""}],""name"":""尺码"",""hasImage"":""false"",""pid"":""20549"",""valueMap"":{""9181973233"":{""comboPropertyValue"":""false"",""vid"":""9181973233"",""sortOrder"":1,""name"":""5=欧码36=22cm""},""9181973234"":{""comboPropertyValue"":""false"",""vid"":""9181973234"",""sortOrder"":2,""name"":""6=欧码37=23cm""},""9181973235"":{""comboPropertyValue"":""false"",""vid"":""9181973235"",""sortOrder"":3,""name"":""7=欧码38=24cm""},""14360986509"":{""comboPropertyValue"":""false"",""vid"":""14360986509"",""sortOrder"":4,""name"":""8=欧码39=25cm""},""9111157657"":{""comboPropertyValue"":""false"",""vid"":""9111157657"",""sortOrder"":5,""name"":""9=欧码40=26cm""},""7913434040"":{""comboPropertyValue"":""false"",""vid"":""7913434040"",""sortOrder"":6,""name"":""10=欧码41=27cm""},""7913434041"":{""comboPropertyValue"":""false"",""vid"":""7913434041"",""sortOrder"":7,""name"":""11=欧码42=28cm""},""7913434042"":{""comboPropertyValue"":""false"",""vid"":""7913434042"",""sortOrder"":8,""name"":""12=欧码43=29cm""}}}]},""pcTrade"":{""buyNowUrl"":""//buy.taobao.com/auction/buy_now.jhtml"",""bizDataBuyParams"":{},""pcCartParam"":{""xxc"":""taobaoSearch"",""areaId"":""110108"",""addressId"":""7689479327""},""pcBuyParams"":{""virtual"":""false"",""buy_now"":""1490.00"",""auction_type"":""b"",""x-uid"":"""",""title"":""UGG2025秋冬设计师款蒂茨库特 长毛皮毛一体拖鞋1173930"",""buyer_from"":""ecity"",""page_from_type"":""main_site_pc"",""detailIsLimit"":""false"",""who_pay_ship"":""卖家承担运费"",""rootCatId"":""50006843"",""routeToNewPc"":""1"",""auto_post"":""false"",""seller_nickname"":""胖胖小丸子海外购"",""photo_url"":""i1/1013510066/O1CN01PNCWJf1CMHEmiWY0k_!!1013510066.jpg"",""current_price"":""1490.00"",""region"":""美国"",""seller_id"":""65731b5ac5774a64344a7caf3a941424"",""etm"":""""},""tradeType"":1},""componentsVO"":{""headerVO"":{""logoJumpUrl"":""https://www.taobao.com"",""mallLogo"":""https://gw.alicdn.com/imgextra/i1/O1CN01z163bz1lHF5yQ50CC_!!6000000004793-2-tps-172-108.png"",""searchText"":""搜索宝贝"",""buttons"":[{""subTitle"":{},""background"":{""alpha"":""1.0"",""disabledAlpha"":""1.0""},""disabled"":""false"",""title"":{""text"":""搜索""},""type"":""search_in_taobao"",""events"":[{""type"":""onClick"",""fields"":{""url"":""//s.taobao.com/search""}}]},{""subTitle"":{},""background"":{""alpha"":""1.0"",""disabledAlpha"":""1.0""},""disabled"":""false"",""title"":{""text"":""搜本店""},""type"":""search_in_store"",""events"":[{""type"":""onClick"",""fields"":{""url"":""//shop297488024.taobao.com/search.htm""}}]}]},""headImageVO"":{""images"":[""https://img.alicdn.com/imgextra/i1/1013510066/O1CN01PNCWJf1CMHEmiWY0k_!!1013510066.jpg"",""https://img.alicdn.com/imgextra/i2/1013510066/O1CN01YCFjsx1CMHEoa60mU~crop,0,185,1179,1179~_!!1013510066.jpg"",""https://img.alicdn.com/imgextra/i2/1013510066/O1CN01Mjdk221CMHEmrtl9v~crop,0,183,1179,1179~_!!1013510066.jpg"",""https://img.alicdn.com/imgextra/i4/1013510066/O1CN01xEQALV1CMHEiZPBSA~crop,0,187,1179,1179~_!!1013510066.jpg"",""https://img.alicdn.com/imgextra/i3/1013510066/O1CN012pQTb21CMHEpo7CIP_!!1013510066.jpg""],""videos"":[]},""storeCardVO"":{""buttons"":[{""image"":{""imageUrl"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg"",""gifAnimated"":""false""},""disabled"":""false"",""title"":{""text"":""联系客服""},""type"":""customer_service""},{""image"":{""imageUrl"":""https://img.alicdn.com/imgextra/i4/O1CN01jn67ow1ZhYeiTJlZn_!!6000000003226-55-tps-24-24.svg"",""gifAnimated"":""false""},""disabled"":""false"",""title"":{""text"":""进入店铺""},""type"":""enter_shop"",""events"":[{""type"":""openUrl"",""fields"":{""url"":""//shop297488024.taobao.com""}}]}],""overallScore"":""4.3"",""shopIcon"":""//img.alicdn.com/imgextra//65/73/TB1iVQyciMnBKNjSZFzSuw_qVXa.jpg"",""shopName"":""胖胖小丸子海外购"",""evaluates"":[{""score"":""5.0"",""title"":""宝贝质量""},{""score"":""4.4"",""title"":""物流速度""},{""score"":""3.9"",""title"":""服务保障""}],""shopUrl"":""//shop297488024.taobao.com"",""labelList"":[{""contentDesc"":""90天新增43条好评""},{""contentDesc"":""平均2天内发货""},{""contentDesc"":""纠纷率超94%同行""}],""creditLevel"":""10"",""creditLevelIcon"":""//gtms02.alicdn.com/tps/i2/TB1LIf_HpXXXXXWXFXXHpVt.VXX-132-24.png"",""sellerType"":""C"",""starNum"":""4.0""},""titleVO"":{""salesDesc"":""已售 10"",""subTitles"":[],""title"":{""title"":""UGG2025秋冬设计师款蒂茨库特 长毛皮毛一体拖鞋1173930""}},""debugVO"":{""traceId"":""3c05fc0417633719440114527e"",""host"":""taodetail033063213213.center.na620@33.63.213.213""},""rateVO"":{""totalCount"":""0"",""favorableRate"":{}},""umpPriceLogVO"":{""traceId"":""3c05fc0417633719440114527e"",""umpCreateTime"":""2025-11-17 17:32:24"",""xObjectId"":""985061667610"",""type"":99,""bcType"":""c"",""version"":""2.1"",""sId"":""6115344162851"",""bS"":""businessScenario"",""sellerId"":""1013510066"",""dUmpInvoke"":0,""map"":""{6115344162851:{\""channelKeyD\"":\""empty\"",\""fpChannelKeyD\"":\""empty\"",\""price1\"":\""1490.00\"",\""price2\"":\""1308.00\"",\""price3\"":\""1490.00\"",\""sourceTypeKeyD\"":\""4_taobaoSearch\"",\""utcDNow\"":\""20_17900^10_0^5_300\"",\""utcDPre\"":\""noProm\""}}"",""priceTId"":""2147812d17633575580836453e0e50""},""deliveryVO"":{""agingDescColor"":""#FF5000"",""freight"":""运费: 免运费"",""deliveryFromAddr"":""美国"",""addressId"":""7689479327"",""deliverToCity"":""北京"",""areaId"":110108,""deliveryToAddr"":""北京 海淀 西三旗"",""agingDesc"":""预售，7天内发货"",""deliveryToDistrict"":""海淀""},""o2oVo"":{""enableJzLocalizationProduct"":""false""},""itemEndorseVO"":{""endorseList"":[{""textList"":[""超600人加购""],""type"":""itemAddCart""}]},""bottomBarVO"":{""rightButtons"":[{""icon"":{""iconFontName"":""뀚"",""color"":""#666666"",""size"":""14"",""alpha"":""1.0"",""disabledColor"":""#dddddd"",""disabledAlpha"":""1.0""},""disabled"":""false"",""title"":{""color"":""#666666"",""alpha"":""1.0"",""fontSize"":""14"",""bold"":""false"",""text"":""收藏"",""disabledColor"":""#666666"",""disabledAlpha"":""1.0""},""type"":""collect""}],""buyInMobile"":""false"",""leftButtons"":[{""background"":{""gradientColor"":[""#ffcb00"",""#ff9402""],""alpha"":""1.0"",""disabledColor"":[""#ffcb00"",""#ff9402""],""disabledAlpha"":""1.0""},""disabled"":""false"",""title"":{""color"":""#ffffff"",""alpha"":""1.0"",""fontSize"":""16"",""bold"":""true"",""text"":""加入购物车"",""disabledColor"":""#33ffffff"",""disabledAlpha"":""0.2""},""type"":""add_cart""},{""background"":{""gradientColor"":[""#ff7700"",""#ff4900""],""alpha"":""1.0"",""disabledColor"":[""#ff7700"",""#ff4900""],""disabledAlpha"":""1.0""},""disabled"":""false"",""title"":{""color"":""#ffffff"",""alpha"":""1.0"",""fontSize"":""16"",""bold"":""true"",""text"":""领券购买"",""disabledColor"":""#80ffffff"",""disabledAlpha"":""0.2""},""type"":""buy_now""}]},""extensionInfoVO"":{""infos"":[{""title"":""优惠"",""type"":""BIG_MARK_DOWN_COUPON"",""items"":[{""text"":[""官方立减12%省179元""]},{""text"":[""红包减3元""]},{""text"":[""国家贴息3期免息""]}]},{""title"":""保障"",""type"":""GUARANTEE"",""items"":[{""text"":[""大促价保"",""不支持7天无理由退货""]}]},{""title"":""参数"",""type"":""BASE_PROPS"",""items"":[{""text"":[""UGG""],""title"":""品牌""},{""text"":[""欧美风""],""title"":""风格""},{""text"":[""纯羊毛""],""title"":""内里材质""},{""text"":[""5=欧码36=22cm,6=欧码37=23cm,7=欧码38=24cm,8=欧码39=25cm,9=欧码40=26cm,10=欧码41=27cm,11=欧码42=28cm,12=欧码43=29cm""],""title"":""尺码""},{""text"":[""是""],""title"":""是否商场同款""},{""text"":[""包头""],""title"":""鞋头款式""},{""text"":[""少女,中年""],""title"":""适用人群""},{""text"":[""日常""],""title"":""适用场景""},{""text"":[""Disquette Chalet""],""title"":""货号""},{""text"":[""便捷""],""title"":""功能""},{""text"":[""2025年秋季""],""title"":""上市年份季节""},{""text"":[""羊毛""],""title"":""鞋垫材质""},{""text"":[""女""],""title"":""适用性别""},{""text"":[""一脚蹬""],""title"":""闭合方式""},{""text"":[""中""],""title"":""防滑性能""},{""text"":[""蒂茨库特""],""title"":""系列""},{""text"":[""平跟""],""title"":""鞋跟款式""},{""text"":[""EVA""],""title"":""鞋底材质""},{""text"":[""秋冬""],""title"":""适用季节""},{""text"":[""否""],""title"":""是否包跟""},{""text"":[""蓝色/Reef Blue 美国发货,沙堡棕色/Sandcastle 美国发乎,黑色/Black 美国发货,美国直邮，假一赔十""],""title"":""颜色分类""}]},{""title"":""保障"",""type"":""GUARANTEE_NEW"",""items"":[{""icon"":""https://gw.alicdn.com/imgextra/i2/O1CN01KdloOc1iYhaZElYLo_!!6000000004425-2-tps-88-88.png"",""action"":""更多"",""text"":[""订单付款后，若在价保期（在订单详情展示）内降价，可通过“手机淘宝首页搜索-价保中心”申请补差，部分特定场景除外，点击“更多”了解详细规则。""],""title"":""大促价保"",""actionLink"":""https://rulesale.taobao.com/?type=detail&ruleId=10000095&cId=347#/rule/detail?ruleId=10000095&cId=347""},{""icon"":""https://gw.alicdn.com/imgextra/i2/O1CN01rGRSdc27ieaMPmbtb_!!6000000007831-2-tps-88-88.png"",""text"":[""此商品不支持7天无理由退换""],""title"":""不支持7天无理由退货""}]}]},""payVO"":{""payConfigList"":[{""text"":""信用卡支付""}]},""rightBarVO"":{""toolkit"":{""plugin"":{""icon"":""https://gw.alicdn.com/imgextra/i4/O1CN0165n4Cr1CGK2faBVbj_!!6000000000053-1-tps-56-56.gif"",""disabled"":""false"",""href"":""https://pc.taobao.com?channel=item"",""label"":""桌面版"",""priority"":201},""webww2"":{""openType"":""manual"",""icon"":""https://img.alicdn.com/imgextra/i2/O1CN012pqGiT1gp4XhKkkRs_!!6000000004190-2-tps-96-96.png"",""disabled"":""false"",""label"":""联系客服"",""priority"":200},""cart2"":{""icon"":""https://img.alicdn.com/imgextra/i4/O1CN01FOK30u1SymJbsQUtk_!!6000000002316-2-tps-96-96.png"",""disabled"":""false"",""href"":""//cart.taobao.com"",""label"":""购物车"",""priority"":199},""qrcode"":{""priority"":198,""url"":""https://h5.m.taobao.com/awp/core/detail.htm?id=985061667610"",""spm"":""0.0.sidebar.qrcode"",""icon"":""https://img.alicdn.com/imgextra/i3/O1CN01CkZbKp27arsx4ktdK_!!6000000007814-2-tps-96-96.png"",""disabled"":""false"",""href"":""https://h5.m.taobao.com/awp/core/detail.htm?id=985061667610"",""label"":""商品码""},""survey"":{""icon"":""https://gw.alicdn.com/imgextra/i3/O1CN01js47DP1J3DxYBQG4g_!!6000000000972-1-tps-56-56.gif"",""disabled"":""false"",""href"":""https://survey.taobao.com/apps/zhiliao/GUShqv-xp"",""label"":""用户调研"",""priority"":197},""copyUrl"":{""openType"":""manual"",""icon"":""https://img.alicdn.com/imgextra/i1/O1CN01Go6lqn28DnZ3MlmFE_!!6000000007899-2-tps-96-96.png"",""disabled"":""false"",""label"":""复制链接"",""priority"":196},""feedback"":{""priority"":195,""icon"":""https://img.alicdn.com/imgextra/i1/O1CN01at70Km26oJu1Kk0vt_!!6000000007708-2-tps-96-96.png"",""disabled"":""false""},""report"":{""priority"":99,""href"":""//jubao.taobao.com/index.htm?itemId=985061667610&spm=a1z6q.7847058"",""icon"":""https://img.alicdn.com/imgextra/i2/O1CN01RAWBfz20zsCKuENux_!!6000000006921-2-tps-96-96.png"",""label"":""举报"",""disabled"":""false""},""backTop"":{""disabled"":""false"",""priority"":1}},""buyerButtons"":[{""icon"":""https://gw.alicdn.com/imgextra/i4/O1CN0165n4Cr1CGK2faBVbj_!!6000000000053-1-tps-56-56.gif"",""disabled"":""false"",""href"":""https://pc.taobao.com?channel=item"",""label"":""桌面版"",""priority"":201,""type"":""plugin""},{""icon"":""https://img.alicdn.com/imgextra/i2/O1CN012pqGiT1gp4XhKkkRs_!!6000000004190-2-tps-96-96.png"",""disabled"":""false"",""label"":""联系客服"",""priority"":200,""type"":""webww2""},{""icon"":""https://img.alicdn.com/imgextra/i4/O1CN01FOK30u1SymJbsQUtk_!!6000000002316-2-tps-96-96.png"",""disabled"":""false"",""href"":""//cart.taobao.com"",""label"":""购物车"",""priority"":199,""type"":""cart2""},{""icon"":""https://img.alicdn.com/imgextra/i3/O1CN01CkZbKp27arsx4ktdK_!!6000000007814-2-tps-96-96.png"",""disabled"":""false"",""href"":""https://h5.m.taobao.com/awp/core/detail.htm?id=985061667610"",""label"":""商品码"",""priority"":198,""type"":""qrcode""},{""icon"":""https://gw.alicdn.com/imgextra/i3/O1CN01js47DP1J3DxYBQG4g_!!6000000000972-1-tps-56-56.gif"",""disabled"":""false"",""href"":""https://survey.taobao.com/apps/zhiliao/GUShqv-xp"",""label"":""用户调研"",""priority"":197,""type"":""survey""},{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN01Go6lqn28DnZ3MlmFE_!!6000000007899-2-tps-96-96.png"",""disabled"":""false"",""label"":""复制链接"",""priority"":196,""type"":""copyUrl""},{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN01at70Km26oJu1Kk0vt_!!6000000007708-2-tps-96-96.png"",""disabled"":""false"",""priority"":195,""type"":""feedback""},{""icon"":""https://img.alicdn.com/imgextra/i2/O1CN01RAWBfz20zsCKuENux_!!6000000006921-2-tps-96-96.png"",""disabled"":""false"",""href"":""//jubao.taobao.com/index.htm?itemId=985061667610&spm=a1z6q.7847058"",""label"":""举报"",""priority"":99,""type"":""report""},{""disabled"":""false"",""priority"":1,""type"":""backTop""}],""sellerButtons"":[]},""priceVO"":{""extraPrice"":{""priceUnit"":""￥"",""priceColor"":""#FFFFFF"",""priceTitle"":""券后"",""priceColorNew"":""#FFFFFF"",""priceBgColor"":""#FF5000"",""priceTitleColor"":""#FFFFFF"",""priceText"":""1308"",""priceMoney"":""130800"",""hiddenPrice"":""false""},""mainBelt"":{""priceBeltImg"":""https://gw.alicdn.com/imgextra/i2/O1CN01jdtsJI1skHqflajOo_!!6000000005804-0-tps-1500-256.jpg"",""rightBelt"":{""extraTextColor"":""#FFFFFF"",""countdown"":""false"",""extraText"":""结束"",""text"":""11月19日 24点"",""textColor"":""#FFFFFF""},""logo"":""https://img.alicdn.com/i3/O1CN01HU596M1Nfw12DBPZc_!!4611686018427384654-2-atmosphere_center_image_storag-merlin-243-72.png"",""beltStyleType"":2},""price"":{""priceUnit"":""￥"",""priceColor"":""#FF4F00"",""priceTitle"":""优惠前"",""priceColorNew"":""#FFFFFF"",""priceTitleColor"":""#FF4F00"",""priceText"":""1490"",""priceMoney"":""149000"",""hiddenPrice"":""false""},""isNewStyle"":""true""},""webfontVO"":{""enableWebfont"":""false""},""tabVO"":{""tabList"":[{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg"",""name"":""comments"",""sort"":1,""title"":""用户评价""},{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg"",""name"":""base_drops"",""sort"":2,""title"":""参数信息""},{""name"":""factory_qualification"",""sort"":3,""title"":""验厂资质""},{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg"",""name"":""desc"",""sort"":4,""title"":""图文详情""},{""icon"":""https://img.alicdn.com/imgextra/i1/O1CN016DNujx1yMMj6NMXVv_!!6000000006564-55-tps-24-24.svg"",""name"":""recommends"",""sort"":5,""title"":""本店推荐""},{""name"":""guessULike"",""sort"":7,""title"":""看了又看""}]}}},""serverData"":""success"",""ssrItemId"":""985061667610""},""pageConfig"":{}}},""routePath"":""/home"",""matchedIds"":[""home""],""renderMode"":""SSR"",""revalidate"":false}";

            JToken taobaoOriginJObj = JObject.Parse(taobaoOriginJson).SelectToken("loaderData.home.data.res");

            // 创建DTO对象
            ThirdApiWeidianProductOriginObj dto = new ThirdApiWeidianProductOriginObj();
            var detail = dto.Item;

            // 解析基本信息
            var itemData = taobaoOriginJObj.SelectToken("item");
            var sellerData = taobaoOriginJObj.SelectToken("seller");
            var skuData = taobaoOriginJObj.SelectToken("skuCore.sku2info").ToObject<JObject>();
            var skuPropMappingList = taobaoOriginJObj.SelectToken("skuBase.skus").ToObject<JArray>();
            var propList = taobaoOriginJObj.SelectToken("skuBase.props").ToObject<JArray>();

            //产品标识
            detail.Num_iid = itemData.SelectToken("itemId").ToString();
            //标题
            detail.Title = itemData.SelectToken("title").ToString();
            //价格
            detail.Price = TypeParseHelper.StrToDecimal(skuData["0"].SelectToken("price.priceText").ToString());

            //主图列表
            JArray imageJArray = itemData.SelectToken("images").ToObject<JArray>();
            if (imageJArray.Any())
            {
                foreach (var image in imageJArray)
                {
                    detail.Item_imgs.Add(new ThirdApiWeidianProductOriginObj_Image
                    {
                        Url = image.ToString()
                    });
                }
            }
            //属性图列表和属性列表
            detail.Prop_imgs = new ThirdApiWeidianProductOriginObj_Propimgs
            {
                Prop_img = new List<ThirdApiWeidianProductOriginObj_Propimgs_Detail>()
            };

            Dictionary<string, string> optionPropDic = new Dictionary<string, string>();

            foreach (JToken prop in propList)
            {
                string optionID = prop["pid"].ToString();
                string optionName = prop["name"].ToString();

                JArray propValues = prop["values"].ToObject<JArray>();
                foreach (JToken propValue in propValues)
                {
                    string choiceID = propValue["vid"].ToString();
                    string choiceName = propValue["name"].ToString();

                    string choiceImg = propValue["image"]?.ToString();

                    //选项卡选项键：选项卡ID:选项ID
                    string optionChoiceKey = $"{optionID}:{choiceID}";
                    //选项卡选项值：选项卡名称:选项名称
                    string optionChoiceValue = $"{optionName}:{choiceName}";

                    if (!string.IsNullOrWhiteSpace(choiceImg))
                    {
                        detail.Prop_imgs.Prop_img.Add(new ThirdApiWeidianProductOriginObj_Propimgs_Detail
                        {
                            Properties = optionChoiceKey,
                            Url = choiceImg
                        });
                    }

                    optionPropDic.Add(optionChoiceKey, optionChoiceValue);
                }
            }
            detail.Props_list = optionPropDic;

            //SKU
            detail.Skus = new ThirdApiWeidianProductOriginObj_Skus
            {
                Sku = new List<ThirdApiWeidianProductOriginObj_Skus_Detail>()
            };

            Dictionary<string, string> skuIDPropMappingDic = new System.Collections.Generic.Dictionary<string, string>();
            foreach (JToken skuPropMapping in skuPropMappingList)
            {
                skuIDPropMappingDic.Add(skuPropMapping["skuId"].ToString(), skuPropMapping["propPath"].ToString());
            }

            foreach (JToken skuJToken in skuPropMappingList)
            {
                string skuID = skuJToken["skuId"].ToString();

                string propMapping = skuJToken["propPath"].ToString();
                string[] propMappingArray = propMapping.Split(';');

                List<string> propIDNameList = new List<string>();
                foreach (var item in propMappingArray)
                {
                    string[] itemArray = item.Split(':');
                    string optionID = itemArray[0];
                    string choiceID = itemArray[1];

                    string optionPropValue = optionPropDic[$"{optionID}:{choiceID}"];
                    string[] optionPropValueArray = optionPropValue.Split(':');

                    string optionName = optionPropValueArray[0];
                    string choiceName = optionPropValueArray[1];
                    propIDNameList.Add($"{optionID}:{choiceID}:{optionName}:{choiceName}");
                }

                JToken skuItemPropObj = skuData[skuID];
                //SKU库存
                int skuQuantity = skuItemPropObj["quantity"].ToObject<int>();
                decimal skuPrice = skuItemPropObj.SelectToken("price.priceText").ToObject<decimal>();

                detail.Skus.Sku.Add(new ThirdApiWeidianProductOriginObj_Skus_Detail
                {
                    Price = skuPrice,
                    Quantity = skuQuantity,
                    Sku_id = skuID,
                    Properties_name = string.Join(';', propIDNameList)
                });
            }

            //销量
            detail.Sales = itemData.SelectToken("vagueSellCount").ToString();
            // 处理卖家信息
            detail.Seller_info = new ThirdApiWeidianProductOriginObj_Sellerinfo
            {
                Shop_name = sellerData.SelectToken("shopName").ToString()
            };
            detail.Seller_id = sellerData.SelectToken("sellerId").ToString();
            detail.Shop_id = sellerData.SelectToken("shopId").ToString();
            //描述
            string descGetUrl = itemData.SelectToken("pcADescUrl").ToString();
            descGetUrl = descGetUrl.StartsWith("//") ? $"https:{descGetUrl}" : descGetUrl;
            //var getDescResult = await this.PayHttpClient.Get(descGetUrl);
            detail.Desc = $@"<iframe src=""{descGetUrl}""></iframe>";
            //产品链接
            detail.Detail_url = $"https://item.taobao.com/item.htm?id={detail.Num_iid}";

            return Ok(dto);
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
