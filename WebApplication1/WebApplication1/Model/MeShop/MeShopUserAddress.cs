namespace WebApplication1.Model.MeShop
{
	/// <summary>
	/// 用户地址
	/// </summary>
	public class MeShopUserAddress
	{
		/// <summary>
		/// 主键ID，自增ID（更新时传值）
		/// </summary>
		public long ID { get; set; }

		/// <summary>
		/// 用户ID
		/// </summary>
		public long UserID { get; set; }

		/// <summary>
		/// 名
		/// </summary>
		public string FirstName { get; set; }

		/// <summary>
		/// 姓
		/// </summary>
		public string LastName { get; set; }

		/// <summary>
		/// 国家地理ID
		/// </summary>
		public long CountryID { get; set; }

		/// <summary>
		/// 省州ID
		/// </summary>
		public long ProvinceID { get; set; }

		/// <summary>
		/// 省州
		/// </summary>
		public string Province { get; set; }

		/// <summary>
		/// 城市
		/// </summary>
		public string City { get; set; }

		/// <summary>
		/// 地址一
		/// </summary>
		public string Address1 { get; set; }

		/// <summary>
		/// 地址二
		/// </summary>
		public string Address2 { get; set; }

		/// <summary>
		/// 邮编
		/// </summary>
		public string ZIP { get; set; }

		/// <summary>
		/// 收件人手机号
		/// </summary>
		public string Phone { get; set; }

		/// <summary>
		/// 地址类型：1=Ship,2=Bill
		/// </summary>
		public int Type { get; set; }

		/// <summary>
		/// 是否为默认地址:1=是，0=否
		/// </summary>
		public int IsDefault { get; set; }

		/// <summary>
		/// 国家简码
		/// </summary>
		public string CountryCode { get; set; }

		/// <summary>
		/// 省州简码
		/// </summary>
		public string ProvinceCode { get; set; }
	}
}
