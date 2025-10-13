using System.Linq;
using System.Threading.Tasks;
using WebApplication1.DB.Base;
using WebApplication1.DB.CMS;
using WebApplication1.Helper;

namespace WebApplication1.DB.Repository
{
    /// <summary>
    /// 三方产品列表仓储类
    /// </summary>
    public class Wd_ThirdProductListRepository : BaseRepository<Wd_ThirdProductList, string>
    {
        public Wd_ThirdProductListRepository(ConfigHelper configHelper) : base(configHelper)
        {
        }

        public async Task<bool> AddListAsync(params Wd_ThirdProductList[] productLists)
        {
            int insertResult = await this.InsertListAsync(Enum.EDBSiteName.Kaybuy, productLists.ToList());
            return insertResult > 0;
        }

        public async Task<bool> DeleteCollAsync(string collName)
        {
            int delResult = await this.Delete(Enum.EDBSiteName.Kaybuy, m => m.Wt_OriginCollName == collName);
            return delResult > 0;
        }
    }
}
