using System;
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
			int data = (int)await base.ExecuteScalarAsync(EDBSiteName.CMS, $"SELECT MAX(ID) from dbo.TB_Users WHERE SiteID={siteID}", null);

			return data;
		}

		public async Task<TB_Users> GetModelByID(int siteID, int userID)
		{
			List<TB_Users> dataList = await base.Select(EDBSiteName.CMS, m => m.SiteID == siteID && m.ID == userID);

			return dataList.FirstOrDefault();
		}

		public async Task<TB_Users> GetModelByEmail(int siteID, string userEmail)
		{
			List<TB_Users> dataList = await base.Select(EDBSiteName.CMS, m => m.SiteID == siteID && m.Email == userEmail);

			return dataList.FirstOrDefault();
		}

		/// <summary>
		/// 获取站点最近登录用户列表
		/// </summary>
		/// <param name="siteID"></param>
		/// <param name="lastLoginDate"></param>
		/// <returns></returns>
		public async Task<List<TB_Users>> GetModelBySiteIDAndLastLoginDate(int siteID, DateTime lastLoginDate)
		{
			List<TB_Users> dataList = await base.Select(EDBSiteName.CMS, m => m.SiteID == siteID && m.LastLoginDate > Convert.ToDateTime("2022-09-01"));

			return dataList;
		}

		/// <summary>
		/// 获取站点下过单的用户列表
		/// </summary>
		/// <param name="siteID"></param>
		/// <returns></returns>
		public async Task<List<TB_Users>> GetModelBySiteIDAndHaveOrder(int siteID)
		{
			List<TB_Users> dataList = await base.QueryAsync<TB_Users>(EDBSiteName.CMS, $"SELECT * FROM dbo.TB_Users WHERE SiteID={siteID} AND ID IN (SELECT UserID FROM dbo.TB_Order WHERE SiteID={siteID}");

			return dataList;
		}

		/// <summary>
		/// 获取同步用户
		/// </summary>
		/// <param name="siteID"></param>
		/// <returns></returns>
		public async Task<List<TB_Users>> GetSyncUsers(int siteID)
		{
			List<TB_Users> dataList = await base.QueryAsync<TB_Users>(EDBSiteName.CMS, 
				@$"SELECT ID+100000 as ID,Email,RegDate FROM dbo.TB_Users WHERE SiteID={siteID} 
				AND RegDate>'2022-12-15 16:55:13'
				AND ID IN (
					SELECT MAX(ID) AS UserID FROM dbo.TB_Users WHERE SiteID={siteID} GROUP BY Email
				)");
			return dataList;
		}

	}
}
