using PPPayReportTools.Excel;

namespace WebApplication1.Model.ExcelModel
{
	/// <summary>
	/// 首云邮箱
	/// </summary>
	public class ExcelShouYunEmail
	{
		[ExcelTitle("最终创建邮箱")]
		public string Email { get; set; }
		[ExcelTitle("最终创建邮箱密码")]
		public string Password { get; set; }
	}
}
