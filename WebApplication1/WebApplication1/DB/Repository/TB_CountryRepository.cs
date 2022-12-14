using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using WebApplication1.DB.Base;
using WebApplication1.DB.CMS;
using WebApplication1.Enum;
using WebApplication1.Helper;

namespace WebApplication1.DB.Repository
{
	public class TB_CountryRepository : BaseRepository<TB_Country, string>
	{
		public TB_CountryRepository(ConfigHelper configHelper) : base(configHelper)
		{
		}

		/// <summary>
		/// 获取所有国家
		/// </summary>
		/// <param name="siteID"></param>
		/// <param name="lastLoginDate"></param>
		/// <returns></returns>
		public async Task<List<TB_Country>> GetModelAll()
		{
			List<TB_Country> dataList = await base.Select(EDBConnectionType.SqlServer);

			return dataList;
		}
	}
}
