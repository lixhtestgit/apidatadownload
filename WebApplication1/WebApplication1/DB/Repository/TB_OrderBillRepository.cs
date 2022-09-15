using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication1.DB.Base;
using WebApplication1.DB.CMS;
using WebApplication1.Enum;
using WebApplication1.Helper;

namespace WebApplication1.DB.Repository
{
    public class TB_OrderBillRepository : BaseRepository<TB_OrderBill, string>
    {
        public TB_OrderBillRepository(ConfigHelper configHelper) : base(configHelper)
        {
        }

        public async Task<int> GetMaxID(int siteID)
        {
            int data = (int)await base.ExecuteScalar(EDBConnectionType.SqlServer, $"SELECT MAX(ID) from dbo.TB_OrderBill WHERE SiteID={siteID}", null);

            return data;
        }

        public async Task<List<TB_OrderBill>> GetModelByOrderID(int siteID, int orderID)
        {
            List<TB_OrderBill> dataList = await base.Select(EDBConnectionType.SqlServer, m => m.SiteID == siteID && m.OrderID == orderID);

            return dataList;
        }
    }
}
