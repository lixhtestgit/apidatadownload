using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using PPPayReportTools.Excel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Enum;
using WebApplication1.Helper;
using WebApplication1.Model.MeShop;

namespace WebApplication1.Controllers
{
    [Route("api/ProductImageMatch")]
	[ApiController]
	public class ProductImageMatchController : ControllerBase
	{
		public ExcelHelper ExcelHelper { get; set; }
		public ImageHelper ImageHelper { get; }
		public IWebHostEnvironment WebHostEnvironment { get; set; }
		public ILogger Logger { get; set; }
		public IConfiguration Configuration { get; set; }

		private MeShopHelper _meShopHelper;

		public ProductImageMatchController(
			ExcelHelper excelHelper,
			ImageHelper imageHelper,
			IWebHostEnvironment webHostEnvironment,
			ILogger<ProductImageMatchController> logger,
			IConfiguration configuration,
			MeShopHelper meShopHelper)
		{
			this.ExcelHelper = excelHelper;
			this.ImageHelper = imageHelper;
			this.WebHostEnvironment = webHostEnvironment;
			this.Logger = logger;
			this.Configuration = configuration;
			this._meShopHelper = meShopHelper;
		}

		/// <summary>
		/// excel处理
		/// api/ProductImageMatch/MatchSkuImage
		/// </summary>
		/// <returns></returns>
		[Route("MatchSkuImage")]
		[HttpGet]
		public async Task<IActionResult> MatchSkuImage()
		{
			string workPath = @"C:\Users\lixianghong\Desktop\广州产品图片匹配";
			string originDataFilePath = $@"{workPath}\广州仓库存明细最新20230413.xlsx";
			string dataFilePath = $@"{workPath}\数据整理.xlsx";

			List<ExcelLinShi> dataList = null;
			List<ExcelLinShi> newDataList = new List<ExcelLinShi>(10000);
			if (System.IO.File.Exists(dataFilePath))
			{
				dataList = this.ExcelHelper.ReadTitleDataList<ExcelLinShi>(dataFilePath, new ExcelFileDescription());
			}
			else
			{
				dataList = this.ExcelHelper.ReadTitleDataList<ExcelLinShi>(originDataFilePath, new ExcelFileDescription());
				string[] skuArray = dataList.Select(m => m.SKU).Distinct().ToArray();
				List<string> hostAdminList = new List<string> { "tbdressshop", "ericdressfashion", "shoespieshop", "wigsbuyshop", "tidebuyshop" };
				List<MeShopSkuImage> skuImageDBList = new List<MeShopSkuImage>(10 * 10000);
				foreach (var hostAdmin in hostAdminList)
				{
					this.Logger.LogInformation($"正在读取{hostAdmin}站点图片数据...");
					List<MeShopSkuImage> dataDBList = await this._meShopHelper.GetProductImageBySKU(hostAdmin, skuArray);
					foreach (MeShopSkuImage dataDB in dataDBList)
					{
						dataDB.HostAdmin = hostAdmin;
					}
					skuImageDBList.AddRange(dataDBList);
				}
				MeShopSkuImage meShopSkuImage = null;
				foreach (ExcelLinShi skuImage in dataList)
				{
					if (!newDataList.Exists(m => $"{m.SKUCode}_{m.SKU}" == $"{skuImage.SKUCode}_{skuImage.SKU}"))
					{
						meShopSkuImage = skuImageDBList.FirstOrDefault(m => m.SKU == skuImage.SKU);
						if (meShopSkuImage != null)
						{
							skuImage.ImageSrc = meShopSkuImage.ImageSrc;
							skuImage.HoadAdmin = meShopSkuImage.HostAdmin;
							skuImage.SPUID = meShopSkuImage.SPUID;
							newDataList.Add(skuImage);
						}
					}
				}
				IWorkbook workbook = this.ExcelHelper.CreateOrUpdateWorkbook(newDataList);
				this.ExcelHelper.SaveWorkbookToFile(workbook, dataFilePath);
			}

			bool isDownLoadImage = false;
			if (isDownLoadImage)
			{
				int i = 0;
				int t = newDataList.Count;
				string downLoadPath = $@"{workPath}\DownLoad";
				if (!Directory.Exists(downLoadPath))
				{
					Directory.CreateDirectory(downLoadPath);
				}
				foreach (ExcelLinShi skuImage in newDataList)
				{
					i++;
					string imageName = $"{skuImage.SKUCode}_{skuImage.SKU}";
					string imageExtendName = skuImage.ImageSrc.Split('.').LastOrDefault();
					this.Logger.LogInformation($"正在下载第{i}/{t}个图片...");

					string imageFilePath = $@"{downLoadPath}\{imageName}.{imageExtendName}";
					if (!System.IO.File.Exists(imageFilePath))
					{
						await this.ImageHelper.DownLoadImage(skuImage.ImageSrc, imageFilePath);
					}
				}
				this.Logger.LogInformation($"图片下载完成！请使用文件名打包发送给需求方：库房SKU编码_销售SKUID");
			}
			this.Logger.LogInformation($"处理完成.");

			return Ok();
		}

		/// <summary>
		/// 添加产品系列
		/// api/ProductImageMatch/AddProductColl
		/// </summary>
		/// <returns></returns>
		[Route("AddProductColl")]
		[HttpGet]
		public async Task<IActionResult> AddProductColl()
		{
			string hostAdmin = "tbdressshop";
			List<ExcelLinShi> dataList=this.ExcelHelper.ReadTitleDataList<ExcelLinShi>(@"C:\Users\lixianghong\Desktop\广州产品图片匹配\广州仓库存数据整理20230413.xlsx", new ExcelFileDescription());

			dataList = dataList.FindAll(m => m.HoadAdmin == hostAdmin);
			List<MeShopColl> allCollList = await this._meShopHelper.GetAllColl(hostAdmin);

			MeShopColl currentColl = allCollList.FirstOrDefault(m => m.Title == "wehuo");
			if (currentColl.Type == (int)EMeShopProductCollectionType.自动)
			{
				this.Logger.LogInformation($"自动系列不需要添加产品...");
			}
			else
			{
				long[] spuidArray = dataList.Select(m => m.SPUID).Distinct().ToArray();
				await this._meShopHelper.AddCollProduct(hostAdmin, currentColl.ID, spuidArray);
			}
			return Ok();
		}
	}

	public class ExcelLinShi
	{
		[ExcelTitle("店铺标识")]
		public string HoadAdmin { get; set; }

		[ExcelTitle("产品ID")]
		public long SPUID { get; set; }

		[ExcelTitle("库房SKU编码")]
		public string SKUCode { get; set; }

		[ExcelTitle("销售SKUID")]
		public string SKU { get; set; }

		[ExcelTitle("SKU")]
		public string MeShopSKU
		{
			get
			{
				return this.SKUCode;
			}
		}

		[ExcelTitle("图片地址")]
		public string ImageSrc { get; set; }
	}
}
