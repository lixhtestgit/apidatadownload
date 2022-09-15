using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.DB.Base;
using WebApplication1.DB.CMS;
using WebApplication1.Enum;
using WebApplication1.Helper;

namespace WebApplication1.DB.Repository
{
    public class TB_UsersRepository : BaseRepository<TB_Users, string>
    {
        public TB_UsersRepository(ConfigHelper configHelper) : base(configHelper)
        {
        }

        public async Task<int> GetMaxID(int siteID)
        {
            int data = (int)await base.ExecuteScalar(EDBConnectionType.SqlServer, $"SELECT MAX(ID) from dbo.TB_Users WHERE SiteID={siteID}", null);

            return data;
        }

        public async Task<TB_Users> GetModelByID(int siteID, int userID)
        {
            List<TB_Users> dataList = await base.Select(EDBConnectionType.SqlServer, m => m.SiteID == siteID && m.ID == userID);

            return dataList.FirstOrDefault();
        }
        public async Task<TB_Users> GetModelByEmail(int siteID, string userEmail)
        {
            List<TB_Users> dataList = await base.Select(EDBConnectionType.SqlServer, m => m.SiteID == siteID && m.Email == userEmail);

            return dataList.FirstOrDefault();
        }

    }
}
