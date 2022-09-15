using WebApplication1.DB.Extend;
using WebApplication1.Enum;

namespace WebApplication1.DB.CMS
{
    [ClassMapper(EDBConnectionType.SqlServer, "dbo", "TB_UserSendAddressOrder")]
    public class TB_UserSendAddressOrder
    {
        [PropertyMapper]
        public int OrderID { get; set; }
        [PropertyMapper]
        public int AddressID { get; set; }
        [PropertyMapper]
        public int CountryID { get; set; }
        [PropertyMapper]
        public int SiteID { get; set; }
        [PropertyMapper]
        public string AddRessUserName { get; set; }
    }
}
