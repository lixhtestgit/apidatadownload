using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication1.DB.Base;
using WebApplication1.DB.CMS;
using WebApplication1.Enum;
using WebApplication1.Helper;

namespace WebApplication1.DB.Repository
{
    /// <summary>
    /// 对外推广链接仓储类
    /// </summary>
    public class Wd_PromotionLinkRepository : BaseRepository<Wd_PromotionLink, string>
    {
        public Wd_PromotionLinkRepository(ConfigHelper configHelper) : base(configHelper)
        {
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<(List<Wd_PromotionLink>, int)> GetPageAsync(int pageIndex, int pageSize)
        {
            (List<Wd_PromotionLink>, int) dataList = await base.GetPageAsync<Wd_PromotionLink>(EDBSiteName.Kaybuy, pageIndex, pageSize, "", "");

            return dataList;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(Wd_PromotionLink data)
        {
            return (await base.UpdateAsync(EDBSiteName.Kaybuy, data)) > 0;
        }
    }
}
