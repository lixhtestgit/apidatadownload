using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.DB.Base;
using WebApplication1.DB.CMS;
using WebApplication1.Enum;
using WebApplication1.Helper;

namespace WebApplication1.DB.Repository
{
    public class TB_OrderRepository : BaseRepository<TB_Order, string>
    {
        public TB_OrderRepository(ConfigHelper configHelper) : base(configHelper)
        {
        }

        public async Task<int> GetMaxID(int siteID)
        {
            int data = (int)await base.ExecuteScalar(EDBConnectionType.SqlServer, $"SELECT MAX(ID) from dbo.TB_Order WHERE SiteID={siteID}", null);

            return data;
        }

        public async Task<TB_Order> GetModelByID(int siteID, int orderID)
        {
            List<TB_Order> dataList = await base.Select(EDBConnectionType.SqlServer, m => m.SiteID == siteID && m.ID == orderID);

            return dataList.FirstOrDefault();
        }
    }
}
