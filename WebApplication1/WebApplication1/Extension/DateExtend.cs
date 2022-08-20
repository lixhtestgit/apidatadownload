using System;

namespace WebApplication1.Extension
{
    /// <summary>
    /// 日期扩展
    /// </summary>
    public static class DateExtend
    {
        /// <summary>
        /// 年月日时分秒
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string ToString_yyyyMMddHHmmss(this DateTime? date)
        {
            string result = null;
            if (date != null)
            {
                result = date.Value.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return result ?? "";
        }
        /// <summary>
        /// 年月日时分秒
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string ToString_yyyyMMddHHmmss(this DateTime date)
        {
            string result = null;

            result = date.ToString("yyyy-MM-dd HH:mm:ss");

            return result ?? "";
        }
    }
}
