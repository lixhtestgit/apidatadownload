using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApplication1.Helper;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 订单地址数据检测控制器
    /// </summary>
    [Route("api/Test")]
    [ApiController]
    public class TestController : ControllerBase
    {
        public ExcelHelper ExcelHelper;
        private readonly IWebHostEnvironment WebHostEnvironment;
        public ILogger Logger;

        public TestController(
            ExcelHelper excelHelper,
            IWebHostEnvironment webHostEnvironment,
            ILogger<OrderShipController> logger)
        {
            this.Logger = logger;
            this.ExcelHelper = excelHelper;
            this.WebHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// api/Test/
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpGet]
        public async Task Test()
        {
            List<TestCla> aaList = this.ExcelHelper.ReadTitleDataList<TestCla>(@$"C:\Users\lixianghong\Desktop\新建 XLSX 工作表.xlsx", new ExcelFileDescription());

            Dictionary<string, List<int>> aa = new Dictionary<string, List<int>>();

            foreach (var item in aaList)
            {
                if (!aa.ContainsKey(item.Name))
                {
                    aa.Add(item.Name, new List<int>());
                }
                aa[item.Name].Add(item.JF);
            }

            StringBuilder nameScoreList = new StringBuilder();
            nameScoreList.AppendLine("每人得分列表：");
            foreach (var item in aa)
            {
                nameScoreList.AppendLine($"{item.Key}:{string.Join(',', item.Value)}");
            }

            nameScoreList.AppendLine("------------");

            Dictionary<string, int> sortDic = new Dictionary<string, int>();
            foreach (var item in aa)
            {
                sortDic.Add(item.Key, item.Value.Sum(m => m));
            }
            nameScoreList.AppendLine("总分排序列表：");
            foreach (var item in sortDic.OrderByDescending(m => m.Value))
            {
                nameScoreList.AppendLine($"{item.Key}：{item.Value}");
            }

            string result = nameScoreList.ToString();

            IWorkbook workbook = this.ExcelHelper.CreateOrUpdateWorkbook(aaList);
            this.ExcelHelper.SaveWorkbookToFile(workbook, @$"C:\Users\lixianghong\Desktop\汇总2_{DateTime.Now.ToString("HHmmss")}.xlsx");

            await Task.CompletedTask;
        }
    }

    public class TestCla
    {
        [ExcelTitle("姓名")]
        public string Name { get; set; }
        [ExcelTitle("积分")]
        public int JF { get; set; }
    }
}
