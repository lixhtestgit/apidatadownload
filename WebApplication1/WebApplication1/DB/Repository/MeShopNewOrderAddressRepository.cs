using WebApplication1.DB.Base;
using WebApplication1.DB.MeShop;
using WebApplication1.Helper;

namespace WebApplication1.DB.Repository
{
    /// <summary>
    /// 子单仓储类
    /// </summary>
    public class MeShopNewOrderAddressRepository : BaseRepository<Order_address, string>
    {
        public MeShopNewOrderAddressRepository(ConfigHelper configHelper) : base(configHelper)
        {
        }
    }
}
