using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebApplication1.Enum;
using WebApplication1.ExcelCsv;
using WebApplication1.Helper;
using WebApplication1.Model.ExcelModel;
using WebApplication1.Model.MeShop;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// MeShopSpu控制器
    /// </summary>
    [Route("api/MeShopSpu")]
    [ApiController]
    public class MeShopSpuController : ControllerBase
    {
        public IWebHostEnvironment WebHostEnvironment;
        protected HttpClient PayHttpClient;
        public ExcelHelper ExcelHelper;
        public CsvHelper CsvHelper;
        public ILogger Logger;
        public MeShopHelper MeShopHelper;
        private readonly MeShopNewHelper meShopNewHelper;

        public MeShopSpuController(
            IWebHostEnvironment webHostEnvironment,
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            CsvHelper csvHelper,
            ILogger<MeShopSpuController> logger,
            MeShopHelper meShopHelper,
            MeShopNewHelper meShopNewHelper)
        {
            this.WebHostEnvironment = webHostEnvironment;
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.CsvHelper = csvHelper;
            this.Logger = logger;
            this.MeShopHelper = meShopHelper;
            this.meShopNewHelper = meShopNewHelper;
        }

        /// <summary>
        /// 修改产品状态
        /// api/MeShopSpu/SetProductState
        /// </summary>
        /// <returns></returns>
        [Route("SetProductState")]
        [HttpGet]
        public async Task<IActionResult> SetProductState()
        {
            string dataSourceDirectoryPath = $@"{this.WebHostEnvironment.ContentRootPath}\示例测试目录\产品\产品系列更新";
            string saveFilePath = dataSourceDirectoryPath + "\\产品系列关系.xlsx";
            string hostAdmin = "tidebuyshop";

            List<MeShopColl> allCollList = await this.MeShopHelper.GetAllColl(hostAdmin);
            List<ExcelProductSpu> allExcelProductSpuList = new List<ExcelProductSpu>(10000);
            if (System.IO.File.Exists(saveFilePath))
            {
                allExcelProductSpuList.AddRange(this.ExcelHelper.ReadTitleDataList<ExcelProductSpu>(saveFilePath, new ExcelFileDescription()));
            }
            else
            {
                string[] collDicArray = Directory.GetDirectories(dataSourceDirectoryPath, "*", searchOption: SearchOption.AllDirectories);

                int syncFilePosition = 0;
                int syncTotalFileCount = collDicArray.Length;

                List<string> noMatchCollNameList = new List<string>(100);

                foreach (var collDic in collDicArray)
                {
                    syncFilePosition++;

                    string collTitle = collDic.Split('\\').LastOrDefault();

                    this.Logger.LogInformation($"正在读取第{syncFilePosition}/{syncTotalFileCount}个系列数据...{collTitle}");

                    //读取Excel数据源数据
                    List<ExcelProductSpu> currentExcelProductSpuList = new List<ExcelProductSpu>(10000);
                    string[] productFiles = Directory.GetFiles(collDic, "*", searchOption: SearchOption.AllDirectories);
                    if (productFiles.Length > 1)
                    {
                        continue;
                    }
                    foreach (var productFile in productFiles)
                    {
                        currentExcelProductSpuList.AddRange(this.ExcelHelper.ReadTitleDataList<ExcelProductSpu>(productFile, new ExcelFileDescription(0)));
                    }

                    //从数据库中读取产品ID相关数据
                    List<MeShopSpuDB> currentDBProductSpuList = new List<MeShopSpuDB>(currentExcelProductSpuList.Count);
                    int maxSearchCount = 1000;
                    int pageIndex = 1;
                    List<ExcelProductSpu> searchList = null;
                    do
                    {
                        string inSql = null;
                        searchList = currentExcelProductSpuList.Skip((pageIndex - 1) * maxSearchCount).Take(maxSearchCount).ToList();
                        if (searchList.Count > 0)
                        {
                            inSql = string.Join(',', searchList.Select(m => $"'{m.Handle}'"));
                            string searchSql = $"select ID,SPU,Handle,State from product_spu where Handle in ({inSql})";
                            currentDBProductSpuList.AddRange(await this.MeShopHelper.SelectDataToShop<MeShopSpuDB>(hostAdmin, searchSql));
                        }
                        pageIndex++;
                    } while (searchList.Count > 0);

                    //同步数据库中数据到Excel实体类中
                    List<MeShopColl> currentCollList = allCollList.FindAll(m => m.Title.Equals(collTitle, StringComparison.OrdinalIgnoreCase));
                    if (currentCollList.Count == 0)
                    {
                        noMatchCollNameList.Add(collDic);
                    }
                    else
                    {
                        foreach (var item in currentExcelProductSpuList)
                        {
                            item.SpuID = currentDBProductSpuList.FirstOrDefault(m => m.Handle == item.Handle)?.ID.ToString() ?? "";
                            item.NewCollIDList = currentCollList.Select(m => m.ID.ToString()).ToList();

                            ExcelProductSpu excelProductSpu = allExcelProductSpuList.FirstOrDefault(m => m.SpuID == item.SpuID);
                            if (excelProductSpu == null)
                            {
                                allExcelProductSpuList.Add(item);
                            }
                            else
                            {
                                excelProductSpu.NewCollIDList = excelProductSpu.NewCollIDList.Union(item.NewCollIDList).ToList();
                            }
                        }
                    }
                }

                this.Logger.LogInformation($"正在保存整理后数据...");

                IWorkbook workbook = this.ExcelHelper.CreateOrUpdateWorkbook(allExcelProductSpuList);
                this.ExcelHelper.SaveWorkbookToFile(workbook, saveFilePath);

                if (noMatchCollNameList.Count > 0)
                {
                    throw new Exception($"发现未匹配产品集合：{JsonConvert.SerializeObject(noMatchCollNameList)}");
                }
            }
            allExcelProductSpuList.RemoveAll(m => string.IsNullOrEmpty(m.SpuID));

            long[] allSpuIDArray = allExcelProductSpuList.Select(m => Convert.ToInt64(m.SpuID)).Distinct().ToArray();

            this.Logger.LogInformation($"正在删除系列产品关系...");
            await this.MeShopHelper.DeleteCollProductByProductID(hostAdmin, allSpuIDArray);

            this.Logger.LogInformation($"正在添加系列产品关系...");
            List<string> allCollID = allExcelProductSpuList.SelectMany(m => m.NewCollIDList).Distinct().ToList();
            foreach (var collID in allCollID)
            {
                MeShopColl currentColl = allCollList.FirstOrDefault(m => m.ID.ToString() == collID);
                if (currentColl.Type == (int)EMeShopProductCollectionType.自动)
                {
                    this.Logger.LogInformation($"自动系列不需要添加产品...");
                }
                else
                {
                    long[] addCollSpuIDArray = allExcelProductSpuList.FindAll(m => m.NewCollIDList.Contains(currentColl.ID.ToString())).Select(m => Convert.ToInt64(m.SpuID)).Distinct().ToArray();
                    await this.MeShopHelper.AddCollProduct(hostAdmin, currentColl.ID, addCollSpuIDArray);
                }
            }

            this.Logger.LogInformation($"最后记得手动同步一下所有产品到ES，程序同步可能会出现中断，遗漏等问题...");

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        /// <summary>
        /// 删除产品并清理产品系列关系
        /// api/MeShopSpu/DelCollProduct
        /// </summary>
        /// <returns></returns>
        [Route("DelCollProduct")]
        [HttpGet]
        public async Task<IActionResult> DelCollProduct()
        {
            string hostAdmin = "teamliu5";
            string collName = "goodsale5";

            List<MeShopSpuDB> spuList = null;
            MeShopColl currentColl = null;
            if (collName.IsNotNullOrEmpty())
            {
                List<MeShopColl> allCollList = await this.MeShopHelper.GetAllColl(hostAdmin);
                currentColl = allCollList.FirstOrDefault(m => m.Title == collName);

                spuList = await this.MeShopHelper.GetProductListByCollID(hostAdmin, currentColl.ID);
            }
            else
            {
                spuList = await this.MeShopHelper.GetProductListByCreateTime(hostAdmin, Convert.ToDateTime("2023-5-29 03:00:00"));
                //spuList = await this.MeShopHelper.GetProductList(hostAdmin);
            }

            List<long> spuIDS = spuList.Select(m => m.ID).Distinct().ToList();

            int pageSize = 3000;
            int page = 1;
            long[] spuIDList;
            int delCount = 0;
            do
            {
                spuIDList = spuIDS.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
                if (spuIDList != null && spuIDList.Length > 0)
                {
                    delCount = await this.MeShopHelper.SyncProductStateToShop(hostAdmin, EMeShopProductState.删除, spuIDList);
                    this.Logger.LogInformation($"正在删除系列产品...{(page - 1) * pageSize + spuIDList.Length}/{spuIDS.Count}个");
                }

                if (false && collName.IsNotNullOrEmpty())
                {
                    delCount = await this.MeShopHelper.DeleteCollProductByCollID(hostAdmin, currentColl.ID);
                    this.Logger.LogInformation($"正在删除系列产品关系...{(page - 1) * pageSize + spuIDList.Length}/{spuIDS.Count}个");
                }
                else
                {
                    delCount = await this.MeShopHelper.DeleteCollProductByProductID(hostAdmin, spuIDList);
                    this.Logger.LogInformation($"正在删除系列产品关系...{(page - 1) * pageSize + spuIDList.Length}/{spuIDS.Count}个");
                }
                page++;
            } while (spuIDList.Length == pageSize);

            this.Logger.LogInformation($"最后记得手动同步一下所有产品到ES，程序同步可能会出现中断，遗漏等问题...");

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        /// <summary>
        /// 删除产品并清理产品系列关系
        /// api/MeShopSpu/UpdateHandle
        /// </summary>
        /// <returns></returns>
        [Route("UpdateHandle")]
        [HttpGet]
        public async Task<IActionResult> UpdateHandle()
        {
            List<SpuUpdateHandle> spuHandleList = this.ExcelHelper.ReadTitleDataList<SpuUpdateHandle>(@"C:\Users\lixianghong\Desktop\null_.xlsx", new ExcelFileDescription());
            List<string> updateSqlList = new List<string>(spuHandleList.Count);
            List<string> spuList = new List<string>(spuHandleList.Count);

            Regex updateRegex = new Regex("[^\\w]+");
            Regex gangRegex = new Regex("[-]{2,}");

            string updateSql = null;
            int execCount = 0;

            int index = 0;
            int totalCount = spuHandleList.Count;
            bool syncResult = false;
            string hostAdmin = "thepowers";
            foreach (var item in spuHandleList)
            {
                index++;
                item.Handle = updateRegex.Replace(item.Title, "-");
                item.Handle = gangRegex.Replace(item.Handle, "-").ToLower();
                updateSqlList.Add($"update product_spu set handle='{item.Handle}' where spuid='{item.SpuID}';");
                spuList.Add(item.SpuID);

                if (updateSqlList.Count == 100)
                {
                    updateSql = string.Join("", updateSqlList);
                    execCount = await this.meShopNewHelper.ExecSqlToShop(hostAdmin, 1, updateSql);
                    syncResult = await this.meShopNewHelper.SyncProductDataToES(hostAdmin, spuList);
                    Console.WriteLine($"执行{index}/{totalCount}结果：{execCount},同步ES结果：{syncResult}");
                    updateSqlList.Clear();
                    spuList.Clear();
                }
            }

            updateSql = string.Join("", updateSqlList);
            execCount = await this.meShopNewHelper.ExecSqlToShop("thepowers", 1, updateSql);
            syncResult = await this.meShopNewHelper.SyncProductDataToES(hostAdmin, spuList);
            Console.WriteLine($"执行{totalCount}/{totalCount}结果：{execCount},同步ES结果：{syncResult}");

            return Ok();
        }
    }

    public class SpuUpdateHandle
    {
        [ExcelTitle("spuid")]
        public string SpuID { get; set; }
        [ExcelTitle("title")]
        public string Title { get; set; }
        [ExcelTitle("handle")]
        public string Handle { get; set; }
    }
}
