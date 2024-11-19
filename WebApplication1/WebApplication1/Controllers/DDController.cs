using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyPaymentHelper.Helper;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 调度控制器
    /// </summary>
    [Route("api/DD")]
    [ApiController]
    public class DDController : ControllerBase
    {
        private readonly IWebHostEnvironment WebHostEnvironment;
        public ILogger Logger;
        private readonly DDHelper ddHelper;

        public DDController(
            IWebHostEnvironment webHostEnvironment,
            ILogger<OrderShipController> logger,
            DDHelper ddHelper)
        {
            this.Logger = logger;
            this.ddHelper = ddHelper;
            this.WebHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// api/DD/RestorePayDataByLog
        /// </summary>
        /// <returns></returns>
        [Route("RestorePayDataByLog")]
        [HttpGet]
        public async Task RestorePayDataByLog()
        {
            using (FileStream fileStream = new FileStream(@"C:\Users\lixianghong\Desktop\redis-4.json", FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    string line = null;
                    int lineIndex = 0;
                    string jq = "dootok.com";
                    do
                    {
                        line = await reader.ReadLineAsync();
                        lineIndex++;
                        this.Logger.LogInformation($"正在处理读取第{lineIndex}行数据：" + line);


                        //下面的逻辑有点问题，需要取调度最终数据更新才可以；  还有不能大批量刷新数据
                        //if (line.StartsWith("{") && line.EndsWith("}"))
                        //{
                        //    string tokenData = line;
                        //    string token = JObject.Parse(tokenData).SelectToken("Token").ToString();
                        //    string tokenPayData = await this.ddHelper.GetSessionAsync(jq, token);
                        //    if (string.IsNullOrWhiteSpace(tokenPayData))
                        //    {
                        //        bool updateResult = await this.ddHelper.UpdateSessionAsync(jq, tokenData);
                        //        this.Logger.LogInformation($"更新{token}结果：" + updateResult.ToString());
                        //    }
                        //}
                    } while (!string.IsNullOrWhiteSpace(line));
                }
            }
        }
    }
}
