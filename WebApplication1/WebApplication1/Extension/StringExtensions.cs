namespace System
{
    /// <summary>
    /// String类型扩展
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// 是否为Null或空
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }
        /// <summary>
        /// 是否不为Null或空
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNotNullOrEmpty(this string s)
        {
            return !string.IsNullOrEmpty(s);
        }

        /// <summary>
        /// 获取字符串中第strPosition个位置的str的下标
        /// </summary>
        /// <param name="text"></param>
        /// <param name="str"></param>
        /// <param name="strPosition"></param>
        /// <returns></returns>
        public static int GetIndexOfStr(this string text, string str, int strPosition)
        {
            int strIndex = -1;

            int currentPosition = 0;
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(str) && strPosition >= 1)
            {
                do
                {
                    currentPosition++;
                    if (strIndex == -1)
                    {
                        strIndex = text.IndexOf(str);
                    }
                    else
                    {
                        strIndex = text.IndexOf(str, strIndex + 1);
                    }
                } while (currentPosition < strPosition);
            }

            return strIndex;
        }
    }
}
