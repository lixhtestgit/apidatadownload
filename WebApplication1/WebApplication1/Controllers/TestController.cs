using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WebApplication1.Model;

namespace WebApplication1.Controllers
{
    [Route("api/test")]
    [ApiController]
    public class TestController : ControllerBase
    {
        public ExcelHelper ExcelHelper { get; set; }
        public IWebHostEnvironment WebHostEnvironment { get; set; }
        public ILogger Logger { get; set; }
        public IConfiguration Configuration { get; set; }

        public TestController(ExcelHelper excelHelper, IWebHostEnvironment webHostEnvironment, ILogger<TestController> logger, IConfiguration configuration)
        {
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
            this.Logger = logger;
            this.Configuration = configuration;
        }

        /// <summary>
        /// 将enJSON文建转换为EXCEL发给产品进行翻译
        /// </summary>
        /// <returns></returns>
        [Route("BuildExcelByEnJson")]
        [HttpGet]
        public IActionResult BuildExcelByEnJson()
        {
            string templateName = "Template100501";

            string contentRootPath = this.WebHostEnvironment.ContentRootPath;

            var myDataSectionList = this.Configuration.GetSection($"MyData_{templateName}").GetChildren();

            if (myDataSectionList.Count() == 0)
            {
                throw new Exception($"未找到MyData_{templateName}的配置数据");
            }

            Dictionary<string, List<MeshopExcelModel>> pageCultureListDic = new Dictionary<string, List<MeshopExcelModel>>(0);

            foreach (IConfigurationSection item in myDataSectionList)
            {
                string pageName = item.Key;
                List<MeshopExcelModel> pageList = new List<MeshopExcelModel>(0);

                //添加页面语言对象
                foreach (var pageItemSection in item.GetChildren())
                {
                    string pageItemKey = pageItemSection.Key;
                    string pageKeyEnValue = pageItemSection.Value;
                    if (!pageItemKey.Equals("_title_", StringComparison.OrdinalIgnoreCase))
                    {
                        pageList.Add(new MeshopExcelModel
                        {
                            En = pageKeyEnValue
                        });
                    }
                }
                pageCultureListDic.Add(pageName, pageList);
            }

            IWorkbook workbook = null;
                
            foreach (var pageCultureListItem in pageCultureListDic)
            {
                workbook = ExcelHelper.CreateOrUpdateWorkbook(pageCultureListItem.Value, workbook, sheetName: pageCultureListItem.Key);
            }

            ExcelHelper.SaveWorkbookToFile(workbook, @"C:\Users\lixianghong\Desktop\Test.xlsx");

            return Ok();
        }

        /// <summary>
        /// 将已翻译EXCEL数据根据en.json生成其他多语言json
        /// </summary>
        /// <returns></returns>
        [Route("BuildCultureEXCELToJson")]
        [HttpGet]
        public IActionResult BuildCultureEXCELToJson()
        {
            string templateName = "Template4";

            string contentRootPath = this.WebHostEnvironment.ContentRootPath;

            string testFilePath = null;

            List<MeShopCultureTran> meShopCultureTranList = new List<MeShopCultureTran>(5);
            meShopCultureTranList.Add(new MeShopCultureTran { CultureName = "en", MeShopPageTranList = new List<MeShopPageTran>(0) });
            meShopCultureTranList.Add(new MeShopCultureTran { CultureName = "de", MeShopPageTranList = new List<MeShopPageTran>(0) });
            meShopCultureTranList.Add(new MeShopCultureTran { CultureName = "fr", MeShopPageTranList = new List<MeShopPageTran>(0) });
            meShopCultureTranList.Add(new MeShopCultureTran { CultureName = "ja", MeShopPageTranList = new List<MeShopPageTran>(0) });
            meShopCultureTranList.Add(new MeShopCultureTran { CultureName = "it", MeShopPageTranList = new List<MeShopPageTran>(0) });

            List<MeShopCultureTran> newMeShopCultureTranList = new List<MeShopCultureTran>(5);
            newMeShopCultureTranList.Add(new MeShopCultureTran { CultureName = "en", MeShopPageTranList = new List<MeShopPageTran>(0) });
            newMeShopCultureTranList.Add(new MeShopCultureTran { CultureName = "de", MeShopPageTranList = new List<MeShopPageTran>(0) });
            newMeShopCultureTranList.Add(new MeShopCultureTran { CultureName = "fr", MeShopPageTranList = new List<MeShopPageTran>(0) });
            newMeShopCultureTranList.Add(new MeShopCultureTran { CultureName = "ja", MeShopPageTranList = new List<MeShopPageTran>(0) });
            newMeShopCultureTranList.Add(new MeShopCultureTran { CultureName = "it", MeShopPageTranList = new List<MeShopPageTran>(0) });

            var myDataSectionList = this.Configuration.GetSection($"MyData_{templateName}").GetChildren();

            if (myDataSectionList.Count() == 0)
            {
                throw new Exception($"未找到MyData_{templateName}的配置数据");
            }

            foreach (IConfigurationSection item in myDataSectionList)
            {
                string pageName = item.Key;

                //创建页面对象
                MeShopPageTran enMeShopPageTran = new MeShopPageTran
                {
                    PageName = pageName,
                    PageKeyTranDic = new Dictionary<string, string>(0)
                };
                MeShopPageTran deMeShopPageTran = new MeShopPageTran
                {
                    PageName = pageName,
                    PageKeyTranDic = new Dictionary<string, string>(0)
                };
                MeShopPageTran frMeShopPageTran = new MeShopPageTran
                {
                    PageName = pageName,
                    PageKeyTranDic = new Dictionary<string, string>(0)
                };
                MeShopPageTran jaMeShopPageTran = new MeShopPageTran
                {
                    PageName = pageName,
                    PageKeyTranDic = new Dictionary<string, string>(0)
                };
                MeShopPageTran itMeShopPageTran = new MeShopPageTran
                {
                    PageName = pageName,
                    PageKeyTranDic = new Dictionary<string, string>(0)
                };

                //添加页面语言对象
                foreach (var pageSection in item.GetChildren())
                {
                    string pageKey = pageSection.Key;
                    string pageKeyEnValue = pageSection.Value;
                    string pageKeyCultureValue = "";
                    if (pageKey.Equals("_title_", StringComparison.OrdinalIgnoreCase))
                    {
                        pageKeyCultureValue = pageKeyEnValue;
                    }

                    enMeShopPageTran.PageKeyTranDic.Add(pageKey, pageKeyEnValue);
                    deMeShopPageTran.PageKeyTranDic.Add(pageKey, pageKeyCultureValue);
                    frMeShopPageTran.PageKeyTranDic.Add(pageKey, pageKeyCultureValue);
                    jaMeShopPageTran.PageKeyTranDic.Add(pageKey, pageKeyCultureValue);
                    itMeShopPageTran.PageKeyTranDic.Add(pageKey, pageKeyCultureValue);
                }

                //添加页面对象
                meShopCultureTranList[0].MeShopPageTranList.Add(enMeShopPageTran);
                meShopCultureTranList[1].MeShopPageTranList.Add(deMeShopPageTran);
                meShopCultureTranList[2].MeShopPageTranList.Add(frMeShopPageTran);
                meShopCultureTranList[3].MeShopPageTranList.Add(jaMeShopPageTran);
                meShopCultureTranList[4].MeShopPageTranList.Add(itMeShopPageTran);

            }


            //英语多语言数据
            MeShopCultureTran enCultureTran = meShopCultureTranList[0];
            MeShopCultureTran deCultureTran = meShopCultureTranList[1];
            MeShopCultureTran frCultureTran = meShopCultureTranList[2];
            MeShopCultureTran jaCultureTran = meShopCultureTranList[3];
            MeShopCultureTran itCultureTran = meShopCultureTranList[4];

            List<MeshopExcelModel> meshopExcelModelList = null;

            //测试一：收集单元格数据为对象
            testFilePath = $@"{contentRootPath}\示例测试目录\Meshop-多语言翻译-{templateName}.xlsx";
            IWorkbook workbook = this.ExcelHelper.CreateWorkbook(testFilePath);
            List<ISheet> sheetList = this.ExcelHelper.GetSheetList(workbook);

            StringBuilder checkErrorResult = new StringBuilder();

            foreach (ISheet sheet in sheetList)
            {
                meshopExcelModelList = this.ExcelHelper.ReadTitleList<MeshopExcelModel>(sheet, new ExcelFileDescription(0));

                string pageName = sheet.SheetName.Replace("_", "").Replace(" ", "");

                MeShopPageTran enPageTran = enCultureTran.MeShopPageTranList.Find(m => m.PageName.Replace("_", "").Replace(" ", "").Equals(pageName, StringComparison.OrdinalIgnoreCase));
                MeShopPageTran dePageTran = deCultureTran.MeShopPageTranList.Find(m => m.PageName.Replace("_", "").Replace(" ", "").Equals(pageName, StringComparison.OrdinalIgnoreCase));
                MeShopPageTran frPageTran = frCultureTran.MeShopPageTranList.Find(m => m.PageName.Replace("_", "").Replace(" ", "").Equals(pageName, StringComparison.OrdinalIgnoreCase));
                MeShopPageTran jaPageTran = jaCultureTran.MeShopPageTranList.Find(m => m.PageName.Replace("_", "").Replace(" ", "").Equals(pageName, StringComparison.OrdinalIgnoreCase));
                MeShopPageTran itPageTran = itCultureTran.MeShopPageTranList.Find(m => m.PageName.Replace("_", "").Replace(" ", "").Equals(pageName, StringComparison.OrdinalIgnoreCase));

                if (enPageTran == null || dePageTran == null || frPageTran == null || jaPageTran == null || itPageTran == null)
                {
                    checkErrorResult.AppendLine($"未能从json找到页面Key,detail:pageName={pageName}");
                }
                else
                {
                    MeShopPageTran enPageTran1 = new MeShopPageTran { PageName = enPageTran.PageName, PageKeyTranDic = new Dictionary<string, string>(50) };
                    MeShopPageTran dePageTran2 = new MeShopPageTran { PageName = enPageTran.PageName, PageKeyTranDic = new Dictionary<string, string>(50) };
                    MeShopPageTran frPageTran3 = new MeShopPageTran { PageName = enPageTran.PageName, PageKeyTranDic = new Dictionary<string, string>(50) };
                    MeShopPageTran jaPageTran4 = new MeShopPageTran { PageName = enPageTran.PageName, PageKeyTranDic = new Dictionary<string, string>(50) };
                    MeShopPageTran itPageTran5 = new MeShopPageTran { PageName = enPageTran.PageName, PageKeyTranDic = new Dictionary<string, string>(50) };

                    enPageTran1.PageKeyTranDic.Add("_title_", meShopCultureTranList[0].MeShopPageTranList.Find(m => m.PageName.Replace("_", "").Replace(" ", "").Equals(pageName, StringComparison.OrdinalIgnoreCase)).PageKeyTranDic["_title_"]);
                    dePageTran2.PageKeyTranDic.Add("_title_", meShopCultureTranList[1].MeShopPageTranList.Find(m => m.PageName.Replace("_", "").Replace(" ", "").Equals(pageName, StringComparison.OrdinalIgnoreCase)).PageKeyTranDic["_title_"]);
                    frPageTran3.PageKeyTranDic.Add("_title_", meShopCultureTranList[2].MeShopPageTranList.Find(m => m.PageName.Replace("_", "").Replace(" ", "").Equals(pageName, StringComparison.OrdinalIgnoreCase)).PageKeyTranDic["_title_"]);
                    jaPageTran4.PageKeyTranDic.Add("_title_", meShopCultureTranList[3].MeShopPageTranList.Find(m => m.PageName.Replace("_", "").Replace(" ", "").Equals(pageName, StringComparison.OrdinalIgnoreCase)).PageKeyTranDic["_title_"]);
                    itPageTran5.PageKeyTranDic.Add("_title_", meShopCultureTranList[4].MeShopPageTranList.Find(m => m.PageName.Replace("_", "").Replace(" ", "").Equals(pageName, StringComparison.OrdinalIgnoreCase)).PageKeyTranDic["_title_"]);

                    foreach (MeshopExcelModel meshopExcelModel in meshopExcelModelList)
                    {
                        if (!string.IsNullOrEmpty(meshopExcelModel.En))
                        {
                            List<string> enPageKeyList = enPageTran.PageKeyTranDic.Where(m => m.Value.Replace(" ", "").Equals(meshopExcelModel.En.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)).Select(m => m.Key).ToList();
                            if (enPageKeyList.Count > 0)
                            {
                                foreach (string enPageKey in enPageKeyList)
                                {
                                    dePageTran.PageKeyTranDic[enPageKey] = meshopExcelModel.De;
                                    frPageTran.PageKeyTranDic[enPageKey] = meshopExcelModel.Fr;
                                    jaPageTran.PageKeyTranDic[enPageKey] = meshopExcelModel.Ja;
                                    itPageTran.PageKeyTranDic[enPageKey] = meshopExcelModel.It;

                                    if (!enPageTran1.PageKeyTranDic.ContainsKey(enPageKey))
                                    {
                                        //1-多语言有效性检查：变量一致性检查
                                        Regex paramRegex = new Regex("[{]{1,}[^{}]+[}]{1,}", RegexOptions.IgnoreCase);
                                        string enPraramStr = string.Join("", paramRegex.Matches(meshopExcelModel.En ?? ""));
                                        string dePraramStr = string.Join("", paramRegex.Matches(meshopExcelModel.De ?? ""));
                                        string frPraramStr = string.Join("", paramRegex.Matches(meshopExcelModel.Fr ?? ""));
                                        string jaPraramStr = string.Join("", paramRegex.Matches(meshopExcelModel.Ja ?? ""));
                                        string itPraramStr = string.Join("", paramRegex.Matches(meshopExcelModel.It ?? ""));
                                        if (enPraramStr != dePraramStr
                                            || enPraramStr != frPraramStr
                                            || enPraramStr != jaPraramStr
                                            || enPraramStr != itPraramStr)
                                        {
                                            checkErrorResult.AppendLine($"翻译变量被修改,detail:pageName={pageName},{enPraramStr}_{dePraramStr}_{frPraramStr}_{jaPraramStr}_{itPraramStr}");
                                            continue;
                                        }

                                        //2-多语言文本有效性检查
                                        Dictionary<string, Regex> validCheckDic = new Dictionary<string, Regex>();
                                        validCheckDic.Add("换行符检查", new Regex("[\n]+"));

                                        foreach (var checkItem in validCheckDic)
                                        {
                                            Regex checkRegex = checkItem.Value;
                                            string cultureJoinStr = meshopExcelModel.En + meshopExcelModel.De + meshopExcelModel.Fr + meshopExcelModel.Ja + meshopExcelModel.It;
                                            if (checkRegex.IsMatch(cultureJoinStr))
                                            {
                                                checkErrorResult.AppendLine($"换行符错误,details:pageName={pageName},enPageKey={enPageKey},En={meshopExcelModel.En}");
                                                continue;
                                            }
                                        }

                                        enPageTran1.PageKeyTranDic.Add(enPageKey, meshopExcelModel.En);
                                        dePageTran2.PageKeyTranDic.Add(enPageKey, meshopExcelModel.De);
                                        frPageTran3.PageKeyTranDic.Add(enPageKey, meshopExcelModel.Fr);
                                        jaPageTran4.PageKeyTranDic.Add(enPageKey, meshopExcelModel.Ja);
                                        itPageTran5.PageKeyTranDic.Add(enPageKey, meshopExcelModel.It);
                                    }
                                }
                            }
                            else
                            {
                                checkErrorResult.AppendLine($"无法根据英文找到对应key,detail:{pageName}_{meshopExcelModel.En}");
                                continue;
                            }
                        }
                    }

                    newMeShopCultureTranList[0].MeShopPageTranList.Add(enPageTran1);
                    newMeShopCultureTranList[1].MeShopPageTranList.Add(dePageTran2);
                    newMeShopCultureTranList[2].MeShopPageTranList.Add(frPageTran3);
                    newMeShopCultureTranList[3].MeShopPageTranList.Add(jaPageTran4);
                    newMeShopCultureTranList[4].MeShopPageTranList.Add(itPageTran5);
                }
            }

            //找出所有英语对应key没有对应多语言的词
            foreach (MeShopCultureTran cultureItem in meShopCultureTranList)
            {
                foreach (MeShopPageTran pageItem in cultureItem.MeShopPageTranList)
                {
                    foreach (KeyValuePair<string, string> keyItem in pageItem.PageKeyTranDic)
                    {
                        if (string.IsNullOrEmpty(keyItem.Value))
                        {
                            checkErrorResult.AppendLine($"未翻译错误,details:{cultureItem.CultureName}_{pageItem.PageName}_{keyItem.Key}");
                            continue;
                        }
                    }
                }
            }

            //错误检查
            if (checkErrorResult.Length > 0)
            {
                return Content(checkErrorResult.ToString(), "application/json");
            }
            else
            {
                //打印正常数据
                return Content(JsonConvert.SerializeObject(newMeShopCultureTranList), "application/json");
            }
        }



        public string BuildToInsertSql(List<MeShopCultureTran> newMeShopCultureTranList)
        {
            StringBuilder sqlStringBuilder = new StringBuilder();
            sqlStringBuilder.Append($"TRUNCATE TABLE system_translate; \r\n");
            sqlStringBuilder.Append($"INSERT INTO system_translate VALUES \r\n");

            for (int i = 0; i < newMeShopCultureTranList.Count; i++)
            {
                MeShopCultureTran item = newMeShopCultureTranList[i];

                string pageTranStr = JsonConvert.SerializeObject(item.ShowSqlMeShopPageTranDic);
                string sql = $"('{i + 1}', 'v1', '{item.CultureName}', '0', '0', '{pageTranStr}', 'system', '2020-07-22 15:44:41', 'system', '2020-07-22 15:44:46')";
                if (i < newMeShopCultureTranList.Count - 1)
                {
                    sql += ",\r\n";
                }
                else
                {
                    sql += ";";
                }
                sqlStringBuilder.Append(sql);
            }

            return sqlStringBuilder.ToString();
        }

    }
}
