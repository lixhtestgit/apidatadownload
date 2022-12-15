using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.DB.Base;
using WebApplication1.DB.CMS;
using WebApplication1.Enum;
using WebApplication1.Helper;

namespace WebApplication1.DB.Repository
{
    public class TJ_TB_OrderRepository : BaseRepository<TJ_TB_Order, string>
    {
        public TJ_TB_OrderRepository(ConfigHelper configHelper) : base(configHelper)
        {
        }

        public async Task<int> GetMaxID(int siteID)
        {
            int data = (int)await base.ExecuteScalar(EDBSiteName.CMS, $"SELECT ISNULL(MAX(ID),0) from dbo.TJ_TB_Order WHERE SiteID={siteID}", null);

            return data;
        }

        public async Task<TJ_TB_Order> GetModelByID(int siteID, int orderID)
        {
            List<TJ_TB_Order> dataList = await base.Select(EDBSiteName.CMS, m => m.SiteID == siteID && m.ID == orderID);

            return dataList.FirstOrDefault();
        }
    }
}
