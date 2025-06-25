using PPPayReportTools.Excel;

namespace WebApplication1.Model.ExcelModel
{
    public class ExcelFayBuyProduct
    {
        /// <summary>
        /// 品类
        /// </summary>
        [ExcelTitle("category_url")]
        public string Category_url { get; set; }

        /// <summary>
        /// 产品地址
        /// </summary>
        [ExcelTitle("url")]
        public string Url { get; set; }

        /// <summary>
        /// 需同步的产品价格
        /// </summary>
        [ExcelTitle("价格")]
        public string SyncProductPrice { get; set; }

        /// <summary>
        /// 需同步的产品描述
        /// </summary>
        [ExcelTitle("描述")]
        public string SyncProductDescribtion { get; set; }

        /// <summary>
        /// 需同步的产品图片地址
        /// </summary>
        [ExcelTitle("图片")]
        public string SyncProductImgs { get; set; }

        /// <summary>
        /// 需同步的产品KayBuy地址
        /// </summary>
        [ExcelTitle("Kaybuy链接")]
        public string SyncProductKayBuyUrl { get; set; }

        /// <summary>
        /// 需同步的产品标题
        /// </summary>
        [ExcelTitle("标题")]
        public string SyncProductTitle { get; set; }

        /// <summary>
        /// 需同步的产品原始数据
        /// </summary>
        [ExcelTitle("原始数据")]
        public string SyncProductOriginData { get; set; }
    }
}
