using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.Helper;
using WebApplication1.Model;
using WebApplication1.Model.MeShop;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// MeShopSku控制器
    /// </summary>
    [Route("api/MeShopSku")]
    [ApiController]
    public class MeShopSkuController : ControllerBase
    {
        public IWebHostEnvironment WebHostEnvironment;
        protected HttpClient PayHttpClient;
        public ExcelHelper ExcelHelper;
        public ILogger Logger;
        public MeShopHelper MeShopHelper;

        public MeShopSkuController(
            IWebHostEnvironment webHostEnvironment,
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            ILogger<TestController> logger,
            MeShopHelper meShopHelper)
        {
            this.WebHostEnvironment = webHostEnvironment;
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.Logger = logger;
            this.MeShopHelper = meShopHelper;
        }

        /// <summary>
        /// MeShopSku复制
        /// api/MeShopSku/MeShopSkuCopy
        /// </summary>
        /// <returns></returns>
        [Route("MeShopSkuCopy")]
        [HttpGet]
        public async Task<IActionResult> MeShopSkuCopy()
        {
            string toShopSkuSql = @"select sku.ID,sku.SKU,spu.SPU,sku.SellPrice,sku.MarketPrice,sku.CostPrice
                                    from product_sku sku 
                                    inner join product_spu spu on spu.id=sku.spuid 
                                    where sku.state=1 and spu.state=1";
            List<MeShopSkuDB> toSkuDBList = await this.MeShopHelper.SelectDataToShop<MeShopSkuDB>("janewigshop", toShopSkuSql);

            long minSKU = toSkuDBList.Min(m => m.SKU);
            long maxSKU = toSkuDBList.Max(m => m.SKU);

            string fromShopSkuSql = $@"select sku.ID,sku.SKU,spu.SPU,sku.SellPrice,sku.MarketPrice,sku.CostPrice
                                    from product_sku sku 
                                    inner join product_spu spu on spu.id=sku.spuid 
                                    where sku.SKU>={minSKU} and sku.SKU<={maxSKU} and sku.state=1 and spu.state=1";
            List<MeShopSkuDB> fromSkuDBList = await this.MeShopHelper.SelectDataToShop<MeShopSkuDB>("wigsbuyshop", fromShopSkuSql);

            List<string> updateSqlList = new List<string>(toSkuDBList.Count);

            MeShopSkuDB findSKU = null;
            foreach (MeShopSkuDB skudb in toSkuDBList)
            {
                findSKU = fromSkuDBList.FirstOrDefault(m => m.SPU == skudb.SPU && m.SKU == skudb.SKU);
                if (findSKU != null && (
                    skudb.SellPrice != findSKU.SellPrice
                    || skudb.MarketPrice != findSKU.MarketPrice
                    || skudb.CostPrice != findSKU.CostPrice
                    ))
                {
                    updateSqlList.Add($"update product_sku set sellprice={findSKU.SellPrice},marketprice={findSKU.MarketPrice},costprice={findSKU.CostPrice} where ID={skudb.ID};");
                }
            }

            string updateSql = string.Join("", updateSqlList.ToArray());

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }
    }
}
