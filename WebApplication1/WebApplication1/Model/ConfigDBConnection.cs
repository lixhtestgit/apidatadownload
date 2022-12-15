using WebApplication1.Enum;

namespace WebApplication1.Model
{
    /// <summary>
    /// 数据库连接类
    /// </summary>
    public class ConfigDBConnection
    {
        /// <summary>
        /// 数据连接程序类型
        /// </summary>
        public EDBConnectionType DBConnectionType { get; set; }
		/// <summary>
		/// 站点名称
		/// </summary>
		public EDBSiteName SiteName { get; set; }
		/// <summary>
		/// 链接字符串
		/// </summary>
		public string ConnectionStr { get; set; }
    }
}
