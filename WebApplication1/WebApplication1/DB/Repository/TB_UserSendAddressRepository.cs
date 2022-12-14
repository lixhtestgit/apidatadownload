using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication1.DB.Base;
using WebApplication1.DB.CMS;
using WebApplication1.Enum;
using WebApplication1.Helper;

namespace WebApplication1.DB.Repository
{
	public class TB_UserSendAddressRepository : BaseRepository<TB_UserSendAddress, string>
	{
		public TB_UserSendAddressRepository(ConfigHelper configHelper) : base(configHelper)
		{
		}

		public async Task<List<TB_UserSendAddress>> GetModelByUserIDS(int siteID)
		{
			List<TB_UserSendAddress> dataList = await base.QueryAsync<TB_UserSendAddress>(EDBConnectionType.SqlServer, 
				$@"SELECT * FROM TB_UserSendAddress 
					WHERE SiteID={siteID} 
					AND State=1
					AND UserID IN (
						SELECT ID FROM dbo.TB_Users WHERE SiteID={siteID} 
						AND ID IN (
							SELECT MAX(ID) AS UserID FROM dbo.TB_Users WHERE SiteID={siteID} 
							AND (LastLoginDate>'2022-10-01' OR ID IN (SELECT UserID FROM dbo.TB_Order WHERE SiteID={siteID})
						) GROUP BY Email)
						AND Email NOT LIKE '%tidebuy.net'
					)
					");

			return dataList;
		}
	}
}
