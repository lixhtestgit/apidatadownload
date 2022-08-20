namespace WebApplication1.Extension
{
    /// <summary>
    /// Int扩展
    /// </summary>
    public static class IntExtend
    {
        /// <summary>
        /// 取正值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static decimal ToZheng(this decimal value)
        {
            if (value < 0)
            {
                value = -1 * value;
            }
            return value;
        }

        /// <summary>
        /// 取负值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static decimal ToFu(this decimal value)
        {
            if (value > 0)
            {
                value = -1 * value;
            }
            return value;
        }

    }
}
