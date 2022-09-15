using System;
using WebApplication1.DB.Extend;
using WebApplication1.Enum;

namespace WebApplication1.DB.CMS
{
    [ClassMapper(EDBConnectionType.SqlServer, "dbo", "TB_Users")]
    public class TB_Users
    {
        [PropertyMapper]
        public int ID { get; set; }
        [PropertyMapper]
        public string Password { get; set; }
        [PropertyMapper]
        public string Email { get; set; }
        [PropertyMapper]
        public DateTime RegDate { get; set; }
        [PropertyMapper]
        public string RegIP { get; set; }
        [PropertyMapper]
        public int State { get; set; }
        [PropertyMapper]
        public int UserType { get; set; }
        [PropertyMapper]
        public int UserLevel { get; set; }
        [PropertyMapper]
        public int SiteID { get; set; }
        [PropertyMapper]
        public string InviteCode { get; set; }
    }
}
