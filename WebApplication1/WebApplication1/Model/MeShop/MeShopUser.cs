namespace WebApplication1.Model.MeShop
{
	/// <summary>
	/// 店铺订单
	/// </summary>
	public class MeShopUser
	{
		/// <summary>
		/// ID
		/// </summary>
		public long ID { get; set; }
		/// <summary>
		/// 用户邮箱
		/// </summary>
		public string Email { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }

		public string Phone { get; set; }

		/// <summary>
		/// 用户类型枚举：0=普通用户，1=无注册用户
		/// </summary>
		public int RegisterType { get; set; }
	}
}
