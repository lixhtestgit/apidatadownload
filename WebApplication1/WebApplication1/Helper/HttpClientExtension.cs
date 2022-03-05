namespace System.Net.Http
{
    /// <summary>
    /// https://www.cnblogs.com/wzk153/p/10945313.html
    /// 稳定版HttpClient使用帮助类，区别于dotnet framework的版本。wangyunpeng
    /// </summary>
    public static partial class HttpClientExtension
    {
#if DEBUG
        /// <summary>
        /// 开发环境为方便调试，设置双倍等待时间
        /// </summary>
        private const int MILLISECONDS_DELAY = 1000 * 120;
#else
        /// <summary>
        /// 等待多长时间取消http请求，单位：毫秒
        /// 支付业务有其特殊性：需要和国外服务器交互，暂定30秒超时时间，请不要随意修改，修改请告知lixh。 --20211126
        /// 支付业务有其特殊性：需要和调度和网站交互，新增30秒超时时间，请不要随意修改，修改请告知lixh。 --20210720
        /// </summary>
        private const int MILLISECONDS_DELAY = 1000 * 60;
#endif
    }
}
