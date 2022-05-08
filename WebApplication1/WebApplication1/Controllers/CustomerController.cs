using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebApplication1.Model;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 会计师数据收集控制器
    /// </summary>
    [Route("api/Customer")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        protected HttpClient PayHttpClient { get; set; }
        public ExcelHelper ExcelHelper { get; set; }
        public ILogger Logger { get; set; }

        public static string ExistExcelFilePath = @"C:\Users\lixianghong\Desktop\20220508053232.xlsx";
        public static string CustomerDetailUrl = "http://newims.lekaowang.cn/Site/Sale/ModifyPublicList.aspx?CustomerID={CustomerID}&RealName={RealName}&Tel={Tel}";
        public static string CustomerListUrl = "http://newims.lekaowang.cn/InterfaceLibrary/Sale/Handler_List.ashx?FunName=GetPublicList&Conditions=AllCondition&SearchField=Tel&Method=%E6%8E%A8%E5%B9%BF%E9%A1%B5&ExtendSubject=%E4%B8%AD%E7%BA%A7%E4%BC%9A%E8%AE%A1&CustomerType=%E6%99%AE%E9%80%9A%E6%95%B0%E6%8D%AE";
        public static int PageSize = 1000;
        public static int TotalCount = 0;
        public static Dictionary<string, string> HeadDic = new Dictionary<string, string>
        {
            { "Cookie","LKNewCRMUserEmail=wenjie.cui1@sinodq.net;aliyungf_tc=fe985142363e8f52f1167d51d36c95829d55bc0ed1e2c724ff7a59e8c2d4ef2c;CheckCode=96964;ASP.NET_SessionId=qpy0vvhzr5t4kjlxsame4qgs"}
        };
        public static List<Customer> DataList { get; set; }
        public static List<Customer> SourceDataList { get; set; }

        public CustomerController(
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            ILogger<TestController> logger)
        {
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.Logger = logger;
        }

        [Route("")]
        [HttpGet]
        public IActionResult ESSearchOrderPayType()
        {
            CustomerController.DataList = ExcelHelper.ReadTitleDataList<Customer>(CustomerController.ExistExcelFilePath, new ExcelFileDescription());
            CustomerController.SourceDataList = new List<Customer>(300000);
            IWorkbook workbook = null;

            var getPageResult = this.PayHttpClient.Post(CustomerController.CustomerListUrl, new Dictionary<string, string>
            {
                {"_search","false" },
                {"nd","1651849194124" },
                {"page", "1"},
                {"rows",CustomerController.PageSize.ToString() },
                {"sidx","RepeatDate" },
                {"sord","desc" }
            }, CustomerController.HeadDic).Result;

            CustomerController.TotalCount = JObject.Parse(getPageResult.Item2).SelectToken("records").ToObject<int>();
            int maxPageCount = JObject.Parse(getPageResult.Item2).SelectToken("total").ToObject<int>();

            HashSet<string> customerIDHashSet = new HashSet<string>(CustomerController.TotalCount);
            List<Customer> removeCFList = new List<Customer>(CustomerController.TotalCount);
            foreach (var item in CustomerController.DataList)
            {
                if (customerIDHashSet.Contains(item.CustomerID) == false)
                {
                    customerIDHashSet.Add(item.CustomerID);
                    removeCFList.Add(item);
                }
            }
            CustomerController.DataList = removeCFList;

            #region 多线程

            //int theadCount = 50;
            //int oneTheadPageCount = maxPageCount / theadCount;
            //List<Task> allTask = new List<Task>(theadCount);

            //for (int i = 1; i <= theadCount; i++)
            //{
            //    int beginPage = (i - 1) * oneTheadPageCount + 1;
            //    int endPage = i * oneTheadPageCount;
            //    if (i == theadCount)
            //    {
            //        endPage = maxPageCount;
            //    }
            //    allTask.Add(this.GetDataList($"线程{i}", beginPage, endPage, Order1Controller.PageSize));
            //}

            //bool allTaskIsComplate = true;
            //do
            //{
            //    allTaskIsComplate = true;
            //    foreach (Task item in allTask)
            //    {
            //        if (item.IsCompleted == false)
            //        {
            //            allTaskIsComplate = false;
            //        }
            //    }
            //} while (allTaskIsComplate == false);

            #endregion

            #region 单线程

            this.GetDataList("线程1", 1, maxPageCount, CustomerController.PageSize).Wait();

            #endregion

            string filePath = $@"C:\Users\lixianghong\Desktop\{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";

            CustomerController.DataList = CustomerController.DataList.OrderByDescending(m => m.RepeatDate).ToList();

            workbook = ExcelHelper.CreateOrUpdateWorkbook(CustomerController.DataList);
            ExcelHelper.SaveWorkbookToFile(workbook, filePath);

            workbook = ExcelHelper.CreateOrUpdateWorkbook(CustomerController.SourceDataList);
            ExcelHelper.SaveWorkbookToFile(workbook, $@"C:\Users\lixianghong\Desktop\未去重_{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        [Route("UpdateCustomerPhone")]
        [HttpGet]
        public async Task UpdateCustomerPhone()
        {
            List<Customer> dataList = ExcelHelper.ReadTitleDataList<Customer>(CustomerController.ExistExcelFilePath, new ExcelFileDescription());
            List<Customer> emptyPhoneList = dataList.FindAll(m => string.IsNullOrEmpty(m.Phone));

            Regex phoneRegex = new Regex("(?<=\")\\d{9,13}(?=\")");
            string customerDetailUrl = CustomerController.CustomerDetailUrl;
            foreach (Customer cus in emptyPhoneList)
            {
                customerDetailUrl = customerDetailUrl.Replace("{CustomerID}", cus.CustomerID).Replace("{RealName}", cus.RealName).Replace("{Tel}", cus.Tel);
                var getResult = await this.PayHttpClient.Get(customerDetailUrl);
                cus.Phone = phoneRegex.Match(getResult.Item2).Value;
            }
            IWorkbook workbook = ExcelHelper.CreateOrUpdateWorkbook(dataList);
            ExcelHelper.SaveWorkbookToFile(workbook, $@"C:\Users\lixianghong\Desktop\{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");

            this.Logger.LogInformation($"任务结束.");
        }

        public async Task GetDataList(string name, int minPage, int maxPage, int pageSize)
        {
            int page = minPage;

            Regex phoneRegex = new Regex("(?<=\")\\d{9,13}(?=\")");

            do
            {
                this.Logger.LogInformation($"{name}正在查询{page}/{maxPage}页数据...");
                var rr = await this.PayHttpClient.Post(CustomerController.CustomerListUrl, new Dictionary<string, string>
                {
                    {"_search","false" },
                    {"nd","1651849194124" },
                    {"rows",pageSize.ToString() },
                    {"page",page.ToString() },
                    {"sidx","RepeatDate" },
                    {"sord","desc" }
                }, new Dictionary<string, string>
                {
                    { "Cookie","LKNewCRMUserEmail=wenjie.cui1@sinodq.net;aliyungf_tc=fe985142363e8f52f1167d51d36c95829d55bc0ed1e2c724ff7a59e8c2d4ef2c;CheckCode=96964;ASP.NET_SessionId=qpy0vvhzr5t4kjlxsame4qgs"}
                });

                List<Customer> dataJArray = JObject.Parse(rr.Item2).SelectToken("rows").ToObject<List<Customer>>();
                int cusIndex = 1;
                Customer data = null;
                foreach (Customer cus in dataJArray)
                {
                    this.Logger.LogInformation($"{name}正在查询{page}/{maxPage}页第{cusIndex + (page - 1) * pageSize}/{CustomerController.TotalCount}个_手机号数据...");
                    data = CustomerController.DataList.FirstOrDefault(m => m.CustomerID == cus.CustomerID);
                    if (data != null)
                    {
                        cus.Phone = data.Phone;
                    }

                    if (string.IsNullOrEmpty(cus.Phone))
                    {
                        string customerDetailUrl = CustomerController.CustomerDetailUrl.Replace("{CustomerID}", cus.CustomerID).Replace("{RealName}", cus.RealName).Replace("{Tel}", cus.Tel);
                        var getResult = await this.PayHttpClient.Get(customerDetailUrl);
                        cus.Phone = phoneRegex.Match(getResult.Item2).Value;
                    }

                    if (data == null)
                    {
                        //去重列表添加
                        CustomerController.DataList.Add(cus);
                    }
                    //原始列表添加
                    CustomerController.SourceDataList.Add(cus);

                    cusIndex++;
                }
                page++;
            } while (page <= maxPage);
        }
    }
}
