using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var a = AESHelperNew.Decrypt("P1GdwzaJQ3TXgsq9yuSHE3JR+afkTlKUke6NzDKys59QI+/PmLlk/qdm5FvVYCjaDbPHQNe0oled8Hg146vZaWeXY0PPxgSxXY+RxEmh9IHTkS1DfRY09XyzSuviS/VuOrmXeE/DTAFqmfS40gWRR/qu7WYAK5eOrgKQ0kDBwOzcW/Jgw3IjKToqPBEP4Hjc8zsJxRUvG/oxKiAp+x+ePYDT69GZPfhaDcW/NyurkS9/OSfKEhsZyXsLqjjmABtypPDdAEuCRFyZjK1tKldjSLZV0s96JzwmcjSGFXjfuGNpsEwGva5sO1UaRjTfvotw68Ghw4Ji9LbptCMET/UwcDqk3w/3/8lT4VFLqEZrW0hOCP3Nt1zO0nor6GR4htwWtLtSYSUWcvyM1UN23ynegVr/Tj81NmYLnajFv0OO0olMJ20Kdy9b9x3u4M7K9gkBgAFicCXJQeGqF0nrxAbySRD87OkVGAcJUPIX2yCBYviwaIgjlPuQoUtmG0uQ+uWBibH30mg1LSSGxmW77xdYHrqISufBPG9fhC8BpDQ6eVA5QT4qxjuxT5PzE8MuS3LCGUrf3a8fgAzRBhG8PRTCllJHhwOpVpx/62W8HjOe62lXgKxeL87NxQd0zFwfpZSpgKX8hOKdtrXUbOKPAjoNMv7B+/H/OQvHokgXNxxnu2D1/uKIqd8DrHdDXDaKptJJbaEx7y+wq/2fBO8ns8DwEL0j3KMd0AnbKE8gFiLQfGCW8agvmtDru3jAOINxsY+5YSJH5FRlt3L3IW3kiqL1RZFEXeepe2nF/bVl0w+cbUmx40kw/RnI8/Hk3PsqSZ5/Hx3MnoVPNNhhdNvHDJh9xPwnSRwWp3H7feoVycyLJuzKuNbMCjeRP11wPZpz85Xn");


            var b1 = AESHelperNew.Encrypt("123");
            var b2 = AESHelperNew.Decrypt(b1);

            List<TestCla> aaList = this.ExcelHelper.ReadTitleDataList<TestCla>(@$"C:\Users\lixianghong\Desktop\汇总2.xlsx", new ExcelFileDescription());

            IWorkbook workbook = this.ExcelHelper.CreateOrUpdateWorkbook(aaList);
            this.ExcelHelper.SaveWorkbookToFile(workbook, @$"C:\Users\lixianghong\Desktop\汇总2_{DateTime.Now.ToString("HHmmss")}.xlsx");

            await Task.CompletedTask;
        }
    }

    public class TestCla
    {
        [ExcelTitle("交易号")]
        public string TX { get; set; }
        [ExcelTitle("raw_order_json")]
        public string Raw_order_json { get; set; }
        [ExcelTitle("raw_addresses_json")]
        public string Raw_addresses_json { get; set; }

        public JObject AddressJObj
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(this.Raw_order_json) && Raw_order_json.Contains("countryCode"))
                {
                    return JObject.Parse(this.Raw_order_json).SelectToken("shippingAddress").ToObject<JObject>();
                }
                else
                {
                    return JArray.Parse(this.Raw_addresses_json).FirstOrDefault().ToObject<JObject>();
                }
            }
        }

        [ExcelTitle("国家/地区代码")]
        public string CnCode
        {
            get
            {
                return AddressJObj.SelectToken("countryCode")?.ToString();
            }
        }
        [ExcelTitle("联系电话号码")]
        public string Phone
        {
            get
            {
                return AddressJObj.SelectToken("phone")?.ToString();
            }
        }
        [ExcelTitle("国家/地区")]
        public string CnName
        {
            get
            {
                return AddressJObj.SelectToken("countryName")?.ToString();
            }
        }
        [ExcelTitle("邮政编码")]
        public string Zip
        {
            get
            {
                return AddressJObj.SelectToken("zip")?.ToString();
            }
        }
        [ExcelTitle("省/市/自治区/直辖市/特别行政区")]
        public string Provice
        {
            get
            {
                return AddressJObj.SelectToken("province")?.ToString();
            }
        }
        [ExcelTitle("城镇/城市")]
        public string City
        {
            get
            {
                return AddressJObj.SelectToken("city")?.ToString();
            }
        }
        [ExcelTitle("地址第2行/区/临近地区")]
        public string Address2
        {
            get
            {
                return AddressJObj.SelectToken("address2")?.ToString();
            }
        }
        [ExcelTitle("地址第1行")]
        public string Address1
        {
            get
            {
                return AddressJObj.SelectToken("address1")?.ToString();
            }
        }
    }
}
