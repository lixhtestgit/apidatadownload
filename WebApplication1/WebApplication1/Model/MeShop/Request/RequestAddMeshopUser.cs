namespace WebApplication1.Model.MeShop.Request
{
	/// <summary>
	/// 添加用户请求
	/// </summary>
	public class RequestAddMeshopUser
	{
		public MeShopUser User { get; set; }
		public MeShopUserAddress ShipAddress { get; set; }
		public MeShopUserAddress BillAddress { get; set; }
	}
}
