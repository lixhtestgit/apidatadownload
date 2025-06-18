using PPPayReportTools.Excel;

namespace WebApplication1.Model.ExcelModel
{
    public class ExcelFayBuyProduct
    {
        /// <summary>
        /// 图片
        /// </summary>
        [ExcelTitle("img", true)]
        public string Img { get; set; }

        /// <summary>
        /// 产品地址
        /// </summary>
        [ExcelTitle("url")]
        public string Url { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        [ExcelTitle("title")]
        public string Title { get; set; }

        /// <summary>
        /// 系列地址
        /// </summary>
        [ExcelTitle("category_url")]
        public string Category_url { get; set; }

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
        /// 需同步的产品FayBuy地址
        /// </summary>
        [ExcelTitle("Kaybuy链接")]
        public string SyncProductFayBuyUrl { get; set; }

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
