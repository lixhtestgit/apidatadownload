using Microsoft.AspNetCore.Hosting;
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
using WebApplication1.Model.Customer;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 会计师数据收集控制器
    /// </summary>
    [Route("api/Customer")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        public IWebHostEnvironment WebHostEnvironment { get; set; }
        protected HttpClient PayHttpClient { get; set; }
        public ExcelHelper ExcelHelper { get; set; }
        public ILogger Logger { get; set; }

        public static string CustomerDetailBaseUrl = "http://newims.lekaowang.cn/Site/Sale/SaleModifyDistributionList.aspx?CustomerID={CustomerID}&RealName={RealName}&Tel={Tel}";
        public static string CustomerListBaseUrl = "http://newims.lekaowang.cn/InterfaceLibrary/Sale/Handler_List.ashx?FunName=GetPublicList&Conditions=AllCondition&SearchField=Tel&Method={Method}&ExtendSubject={ExtendSubject}&CustomerType=普通数据";
        public static int PageSize = 1000;
        public static Dictionary<string, string> HeadDic = new Dictionary<string, string>
        {
            { "Cookie","aliyungf_tc=1232463e8b48f4d63de8f1b26091be91b07c27c6fed772ea160b31d4daa19e94; CheckCode=03376; ASP.NET_SessionId=5yzjsqo55x1nvswtstzndkzx; LKNewCRMUserEmail=tinglan.cui@sinodq.net"}
        };

        public CustomerController(
            IWebHostEnvironment webHostEnvironment,
            IHttpClientFactory httpClientFactory,
            ExcelHelper excelHelper,
            ILogger<TestController> logger)
        {
            this.WebHostEnvironment = webHostEnvironment;
            this.PayHttpClient = httpClientFactory.CreateClient();
            this.ExcelHelper = excelHelper;
            this.Logger = logger;
        }

        [Route("DownLoad")]
        [HttpGet]
        public async Task<IActionResult> DownLoad()
        {
            string[] methodArray = new string[] { "SEO", "百度短信", "报名入口", "大唐数据", "公众号", "京东", "老客户转介绍", "乐语", "其他", "数据库", "淘宝", "推广页", "无", "信息流", "注册用户", "400", "百度信息流", "今日头条", "现场面审", "畅想云端数据", "乐语录入", "柯达数据", "电信数据", "用友题库", "用友视频", "广点通信息流", "快手信息流", "推广APP", "用友banner", "公开课", "安徽渠道公众号", "微信二维码", "小程序", "乐考校园", "微博粉丝通", "抖音", "模考大赛", "易聊" };
            //string[] extendSubjectArray = new string[] { "无", "初级会计", "初级会计1", "中级会计", "经济师1", "注册安全工程师", "CPA", "管理会计", "税务师", "初级经济师", "中级经济师", "高级经济师", "软考", "FRR", "基金", "银行", "注册会计师", "证券", "期货", "初级银行", "CFA", "中级银行", "一级建造师", "二级建造师", "二级建造师1", "消防工程师", "教师资格证", "注册建造师", "执业药师", "执业医师", "心理咨询师", "公务员", "健康管理师", "婚姻家庭咨询师", "远程教育", "薪税师", "碳排放管理师" };
            string[] extendSubjectArray = new string[] { "初级会计", "初级会计1", "中级会计", "管理会计", "注册会计师", "经济师1", "初级经济师", "中级经济师", "高级经济师" };

            Dictionary<string, string> customerListUrlDic = new Dictionary<string, string>(methodArray.Length * extendSubjectArray.Length);
            foreach (var method in methodArray)
            {
                foreach (var extendSubject in extendSubjectArray)
                {
                    //if (method != "推广页" || extendSubject != "中级会计")
                    //{
                    customerListUrlDic.Add($"{method}+{extendSubject}", CustomerController.CustomerListBaseUrl.Replace("{Method}", method).Replace("{ExtendSubject}", extendSubject));
                    //}
                }
            }

            IWorkbook workbook = null;
            string fileName = null;
            string dataListUrl = null;
            int fileTotalCount = 0;
            int fileMaxPageCount = 0;
            foreach (var customerListUrlItem in customerListUrlDic)
            {
                fileName = $@"{this.WebHostEnvironment.ContentRootPath}\DownLoad\{customerListUrlItem.Key}.xls";

                if (!System.IO.File.Exists(fileName)
                    && !System.IO.File.Exists(fileName.Replace(".xls", ".xlsx")))
                {
                    dataListUrl = customerListUrlItem.Value;
                    List<Customer> allDataList = ExcelHelper.ReadTitleDataList<Customer>(fileName + "_未完成", new ExcelFileDescription());
                    try
                    {
                        var getPageResult = await this.PayHttpClient.Post(dataListUrl, new Dictionary<string, string>
                        {
                            {"_search","false" },
                            {"nd","1651849194124" },
                            {"page", "1"},
                            {"rows",CustomerController.PageSize.ToString() },
                            {"sidx","RepeatDate" },
                            {"sord","desc" }
                        }, CustomerController.HeadDic);

                        fileTotalCount = JObject.Parse(getPageResult.Item2).SelectToken("records").ToObject<int>();
                        fileMaxPageCount = JObject.Parse(getPageResult.Item2).SelectToken("total").ToObject<int>();

                        if (fileTotalCount <= 0)
                        {
                            workbook = ExcelHelper.CreateOrUpdateWorkbook(allDataList);
                            ExcelHelper.SaveWorkbookToFile(workbook, fileName);

                            continue;
                        }
                        else if (fileTotalCount > 65000)
                        {
                            fileName = fileName.Replace(".xls", ".xlsx");
                        }

                        int fileMinPageCount = (allDataList.Count / CustomerController.PageSize) + 1;
                        allDataList.AddRange(this.GetDataList($"{customerListUrlItem.Key}", dataListUrl, fileMinPageCount, fileMaxPageCount, CustomerController.PageSize, fileTotalCount));

                        //数据去重
                        HashSet<string> customerIDHashSet = new HashSet<string>(fileTotalCount);
                        List<Customer> removeCFList = new List<Customer>(fileTotalCount);
                        foreach (var item in allDataList)
                        {
                            if (customerIDHashSet.Contains(item.CustomerID) == false)
                            {
                                customerIDHashSet.Add(item.CustomerID);
                                removeCFList.Add(item);
                            }
                        }
                        allDataList = removeCFList;

                        //数据排序
                        allDataList = allDataList.OrderByDescending(m => m.RepeatDate).ToList();

                        workbook = ExcelHelper.CreateOrUpdateWorkbook(allDataList);
                        ExcelHelper.SaveWorkbookToFile(workbook, fileName);
                    }
                    catch (Exception)
					{
                        fileName += "_未完成";
                        workbook = ExcelHelper.CreateOrUpdateWorkbook(allDataList);
                        ExcelHelper.SaveWorkbookToFile(workbook, fileName);
                    }
                }
            }

            this.Logger.LogInformation($"任务结束.");

            return Ok();
        }

        public List<Customer> GetDataList(string name, string listUrl, int minPage, int maxPage, int pageSize, int totalCount)
        {
            Dictionary<int, List<Customer>> dataListPageDic = new Dictionary<int, List<Customer>>(1000);
            try
            {
                Task.Factory.StartNew(() =>
                {
                    for (int i = minPage; i <= maxPage; i++)
                    {
                        this.Logger.LogInformation($"{name}正在查询{i}/{maxPage}页数据...");
                        dataListPageDic.Add(i, this.GetPageCustomerList(listUrl, pageSize, i).Result);
                    }
                });

                int page = minPage;
                do
                {
                    List<Customer> pageList = new List<Customer>(pageSize);
                    do
                    {
                        if (dataListPageDic.ContainsKey(page))
                        {
                            pageList = dataListPageDic[page];
                        }
                    } while (pageList.Count <= 0);

                    int cusIndex = 1;
                    List<Task> getPhoneTaskList = new List<Task>(1000);
                    foreach (Customer cus in pageList)
                    {
                        this.Logger.LogInformation($"{name}正在查询{page}/{maxPage}页第{cusIndex + (page - 1) * pageSize}/{totalCount}个_手机号数据...");

                        getPhoneTaskList.Add(this.UpdateCustomerPhone(cus));

                        cusIndex++;
                    }

                    bool taskIsComplate = true;
                    do
                    {
                        taskIsComplate = true;
                        foreach (var item in getPhoneTaskList)
                        {
                            if (item.IsCompleted == false)
                            {
                                taskIsComplate = false;
                                break;
                            }
                        }
                    } while (taskIsComplate == false);

                    page++;
                } while (page <= maxPage);
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, "出现错误，临时返回已下载数据...");
            }

            List<Customer> dataList = new List<Customer>(totalCount);
            foreach (var item in dataListPageDic)
            {
                dataList.AddRange(item.Value);
            }

            return dataList;
        }

        public async Task<List<Customer>> GetPageCustomerList(string listUrl, int pageSize, int page)
        {
            var currentPageList = await this.PayHttpClient.Post(listUrl, new Dictionary<string, string>
            {
                {"_search","false" },
                {"nd","1651849194124" },
                {"rows",pageSize.ToString() },
                {"page",page.ToString() },
                {"sidx","RepeatDate" },
                {"sord","desc" }
            }, CustomerController.HeadDic);

            List<Customer> dataJArray = JObject.Parse(currentPageList.Item2).SelectToken("rows").ToObject<List<Customer>>();
            return dataJArray;
        }

        public async Task UpdateCustomerPhone(Customer cus)
        {
            if (string.IsNullOrEmpty(cus.Phone))
            {
                Regex phoneRegex = new Regex("(?<=\")\\d{9,16}(?=\")");
                string customerDetailUrl = CustomerController.CustomerDetailBaseUrl.Replace("{CustomerID}", cus.CustomerID).Replace("{RealName}", cus.RealName).Replace("{Tel}", cus.Tel);
                customerDetailUrl = customerDetailUrl.Replace("{CustomerID}", cus.CustomerID).Replace("{RealName}", cus.RealName).Replace("{Tel}", cus.Tel);
                var getResult = await this.PayHttpClient.Get(customerDetailUrl);
                cus.Phone = phoneRegex.Match(getResult.Item2).Value;
            }
        }

    }
}
