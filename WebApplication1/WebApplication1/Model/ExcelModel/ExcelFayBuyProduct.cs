using WebApplication1.ExcelCsv;

namespace WebApplication1.Model.ExcelModel
{
    public class ExcelFayBuyProduct
    {
        /// <summary>
        /// 分类
        /// </summary>
        [CsvColumn("category_url")]
        public string Category_url { get; set; }

        /// <summary>
        /// 品牌
        /// </summary>
        [CsvColumn("brand")]
        public string Brand { get; set; }

        /// <summary>
        /// 产品地址
        /// </summary>
        [CsvColumn("url")]
        public string Url { get; set; }

        /// <summary>
        /// 需同步的产品价格
        /// </summary>
        [CsvColumn("价格")]
        public string SyncProductPrice { get; set; }

        /// <summary>
        /// 需同步的产品原始价格
        /// </summary>
        [CsvColumn("原始价格")]
        public string SyncProductOriginPrice { get; set; }

        /// <summary>
        /// 需同步的产品描述
        /// </summary>
        [CsvColumn("描述")]
        public string SyncProductDescribtion { get; set; }

        /// <summary>
        /// 需同步的产品图片地址
        /// </summary>
        [CsvColumn("图片")]
        public string SyncProductImgs { get; set; }

        /// <summary>
        /// 需同步的产品KayBuy地址
        /// </summary>
        [CsvColumn("Kaybuy链接")]
        public string SyncProductKayBuyUrl { get; set; }

        /// <summary>
        /// 需同步的产品标题
        /// </summary>
        [CsvColumn("标题")]
        public string SyncProductTitle { get; set; }

        /// <summary>
        /// 需同步的产品原始数据
        /// </summary>
        [CsvColumn("原始数据")]
        public string SyncProductOriginData { get; set; }
    }
}
