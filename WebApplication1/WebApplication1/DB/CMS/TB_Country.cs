using WebApplication1.DB.Extend;
using WebApplication1.Enum;

namespace WebApplication1.DB.CMS
{
	[ClassMapper(EDBConnectionType.SqlServer, "dbo", "TB_Country")]
	public class TB_Country
	{
		[PropertyMapper]
		public int ID { get; set; }
		[PropertyMapper]
		public string CountryName { get; set; }
		[PropertyMapper]
		public string CountryCoding { get; set; }

	}
}
