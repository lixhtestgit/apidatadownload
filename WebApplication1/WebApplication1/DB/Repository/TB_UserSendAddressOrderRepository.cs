using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.DB.Base;
using WebApplication1.DB.CMS;
using WebApplication1.Enum;
using WebApplication1.Helper;

namespace WebApplication1.DB.Repository
{
    public class TB_UserSendAddressOrderRepository : BaseRepository<TB_UserSendAddressOrder, string>
    {
        public TB_UserSendAddressOrderRepository(ConfigHelper configHelper) : base(configHelper)
        {
        }

        public async Task<List<TB_UserSendAddressOrder>> GetModelByOrderID(int siteID, int orderID)
        {
            List<TB_UserSendAddressOrder> dataList = await base.Select(EDBConnectionType.SqlServer, m => m.SiteID == siteID && m.OrderID == orderID);

            return dataList;
        }

    }
}
