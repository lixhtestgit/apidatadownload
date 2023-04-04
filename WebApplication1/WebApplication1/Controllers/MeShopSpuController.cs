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
using System.Threading.Tasks;
using WebApplication1.Enum;
using WebApplication1.ExcelCsv;
using WebApplication1.Helper;
using WebApplication1.Model;
using WebApplication1.Model.ExcelModel;
using WebApplication1.Model.MeShop;
using static WebApplication1.Enum.EMeShopOrder;

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

        public MeShopSpuController(
            IWebHostEnvironment webHostEnvironment,
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            CsvHelper csvHelper,
            ILogger<MeShopSpuController> logger,
            MeShopHelper meShopHelper)
        {
            this.WebHostEnvironment = webHostEnvironment;
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.CsvHelper = csvHelper;
            this.Logger = logger;
            this.MeShopHelper = meShopHelper;
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
            string dataSourceDirectoryPath = $@"C:\Users\lixianghong\Desktop\新建文件夹\产品包";
            string saveFilePath = dataSourceDirectoryPath + "\\产品系列关系.xlsx";
            string hostAdmin = "tbdressshop";

            List<MeShopColl> allCollList = await this.MeShopHelper.GetAllColl(hostAdmin);
            List<ExcelProductSpu> allExcelProductSpuList = new List<ExcelProductSpu>(10000);
            if (System.IO.File.Exists(saveFilePath))
            {
                allExcelProductSpuList.AddRange(this.ExcelHelper.ReadTitleDataList<ExcelProductSpu>(saveFilePath,new ExcelFileDescription()));
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
                    foreach (var productFile in productFiles)
                    {
                        currentExcelProductSpuList.AddRange(this.CsvHelper.Read<ExcelProductSpu>(productFile, new CsvFileDescription(0)));
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

            long[] allSpuIDArray = allExcelProductSpuList.Select(m => Convert.ToInt64(m.SpuID)).Distinct().ToArray();

            this.Logger.LogInformation($"正在删除系列产品关系...");
            await this.MeShopHelper.DeleteCollProduct(hostAdmin, allSpuIDArray);

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
    }
}
