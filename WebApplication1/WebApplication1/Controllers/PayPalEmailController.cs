using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PPPayReportTools.Excel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// PayPalEmail控制器
    /// </summary>
    [Route("api/PayPalEmail")]
    [ApiController]
    public class PayPalEmailController : ControllerBase
    {
        public IWebHostEnvironment WebHostEnvironment;
        public ExcelHelper ExcelHelper;
        public ILogger Logger;

        public PayPalEmailController(
            IWebHostEnvironment webHostEnvironment,
            ExcelHelper excelHelper,
            ILogger<MeShopSpuController> logger)
        {
            this.WebHostEnvironment = webHostEnvironment;
            this.ExcelHelper = excelHelper;
            this.Logger = logger;
        }

        /// <summary>
        /// 修改产品状态
        /// api/PayPalEmail/GetAllEmail
        /// </summary>
        /// <returns></returns>
        [Route("GetAllEmail")]
        [HttpGet]
        public async Task<IActionResult> GetAllEmail()
        {
            string dataSourceDirectoryPath = $@"C:\Users\lixianghong\Desktop\PayPal账号运维\PP资料整理（最新）\PAYPAL——账密总表—8886--祥红.xlsx";

            List<PayPalEmail> paypalEmailList = this.ExcelHelper.ReadTitleDataList<PayPalEmail>(dataSourceDirectoryPath, new ExcelFileDescription(0));

            dataSourceDirectoryPath = @"C:\Users\lixianghong\Desktop\PayPal账号运维\PP资料整理（最新）\PP账号远程账号登录汇总（最新）.xlsx";
            List<PayPalEmail> paypalEmailList2 = this.ExcelHelper.ReadTitleDataList<PayPalEmail>(dataSourceDirectoryPath, new ExcelFileDescription(0));
            paypalEmailList.AddRange(paypalEmailList2);

            string newPath = @"C:\Users\lixianghong\Desktop\邮箱清理确认是否可以永久关闭还是需要保留(2).xlsx";
            List<PayPalEmail> newEmailList = this.ExcelHelper.ReadTitleDataList<PayPalEmail>(newPath, new ExcelFileDescription(0));

            List<string> emailList = new List<string>(100);
            foreach (var item in newEmailList)
            {
                if (paypalEmailList.Exists(m => m.Email.Trim().Equals(item.Email.Trim(), System.StringComparison.OrdinalIgnoreCase)))
                {
                    emailList.Add(item.Email);
                }
            }

            string existEmail = string.Join(',', emailList);

            await Task.CompletedTask;

            return Ok();
        }

    }

    public class PayPalEmail
    {
        [ExcelTitle("PAYPAL账号", isCheckEmpty: true)]
        public string Email { get; set; }
    }
}
