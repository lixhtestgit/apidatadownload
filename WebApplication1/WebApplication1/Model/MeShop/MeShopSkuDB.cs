namespace WebApplication1.Model.MeShop
{
    public class MeShopSkuDB
    {
        public long ID { get; set; }
        public long SKU { get; set; }
        public long SPU { get; set; }
        public decimal SellPrice { get; set; }
        public decimal MarketPrice { get; set; }
        public decimal CostPrice { get; set; }
    }
}
