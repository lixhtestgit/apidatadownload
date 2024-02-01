using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PPPayReportTools.Excel;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication1.Helper;
using WebApplication1.Model.ExcelModel;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
	[ApiController]
	public class OrderImportController : ControllerBase
	{
		private ExcelHelper _excelHelper;
		private MeShopHelper _meShopHelper;
		private ILogger _logger;
		private object _obj = new object();

		public OrderImportController(
			ExcelHelper excelHelper,
			MeShopHelper meShopHelper,
			ILogger<OrderImportController> logger)
		{
			this._excelHelper = excelHelper;
			this._meShopHelper = meShopHelper;
			this._logger = logger;
		}

		/// <summary>
		/// 复制CMS站点数据到独立做账站点订单表中
		/// api/OrderImport/Import
		/// </summary>
		/// <returns></returns>
		[Route("Import")]
		[HttpGet]
		public async Task CopyDataToSite()
		{
			string importOrderFilePath = @"";
			List<ExcelOrderImport> orderImportList = this._excelHelper.ReadTitleDataList<ExcelOrderImport>(importOrderFilePath, new ExcelFileDescription());
			//foreach (ExcelOrderImport excelOrderImport in orderImportList)
			//{
			//	this._usersRepository.Select
			//}

			this._logger.LogInformation("订单处理完成！");
		}
	}
}
