using WebApplication1.DB.Base;
using WebApplication1.DB.MeShop;
using WebApplication1.Helper;

namespace WebApplication1.DB.Repository
{
    /// <summary>
    /// 主单仓储类
    /// </summary>
    public class MeShopNewOrderMasterRepository : BaseRepository<Order_master, string>
    {
        public MeShopNewOrderMasterRepository(ConfigHelper configHelper) : base(configHelper)
        {
        }
    }
}
