using System.Collections.Generic;

namespace WebApplication1.Model.MeShop
{
    internal class MeShopCultureTran
    {
        public string CultureName { get; internal set; }
        public List<MeShopPageTran> MeShopPageTranList { get; internal set; }
    }

    internal class MeShopPageTran
    {
        public string PageName { get; internal set; }
        public Dictionary<string, string> PageKeyTranDic { get; internal set; }
    }
}