namespace WebApplication1.Model
{
    /// <summary>
    /// 数据库连接类
    /// </summary>
    public class ConfigDBConnection
    {
        /// <summary>
        /// 数据连接程序名称
        /// </summary>
        public string ProviderName { get; set; }
        /// <summary>
        /// 链接字符串
        /// </summary>
        public string ConnectionStr { get; set; }
    }
}
