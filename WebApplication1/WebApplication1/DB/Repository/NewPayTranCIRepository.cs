using WebApplication1.DB.Base;
using WebApplication1.DB.MeShop;
using WebApplication1.Helper;

namespace WebApplication1.DB.Repository
{
    /// <summary>
    /// 调度交易CI仓储类
    /// </summary>
    public class NewPayTranCIRepository : BaseRepository<Pay_transaction_ci, string>
    {
        public NewPayTranCIRepository(ConfigHelper configHelper) : base(configHelper)
        {
        }
    }
}
