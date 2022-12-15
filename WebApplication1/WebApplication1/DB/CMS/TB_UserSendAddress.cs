using WebApplication1.DB.Extend;
using WebApplication1.Enum;

namespace WebApplication1.DB.CMS
{
	[ClassMapper(EDBSiteName.CMS, "dbo", "TB_UserSendAddressOrder")]
	public class TB_UserSendAddress
	{
		[PropertyMapper]
		public int SiteID { get; set; }
		[PropertyMapper]
		public int UserID { get; set; }
		[PropertyMapper]
		public string AddRessUserName { get; set; }
		[PropertyMapper]
		public int CountryID { get; set; }
		/// <summary>
		/// 非表字段（自定义SQL使用）
		/// </summary>
		public string CountryCode { get; set; }
		[PropertyMapper]
		public string Code { get; set; }
		[PropertyMapper]
		public string Phone { get; set; }
		[PropertyMapper]
		public int IsDefault { get; set; }
		[PropertyMapper]
		public string AddressLine1 { get; set; }
		[PropertyMapper]
		public string AddressLine2 { get; set; }
		[PropertyMapper]
		public string Province { get; set; }
		[PropertyMapper]
		public string City { get; set; }
		[PropertyMapper]
		public int Type { get; set; }
		[PropertyMapper]
		public string FirstName { get; set; }
		[PropertyMapper]
		public string LastName { get; set; }
		[PropertyMapper]
		public int State { get; set; }
	}
}
