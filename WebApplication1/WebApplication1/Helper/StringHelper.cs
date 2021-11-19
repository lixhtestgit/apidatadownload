using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication1
{
    public class StringHelper
    {
        /// <summary>
        /// 获取字符串中第strPosition个位置的str的下标
        /// </summary>
        /// <param name="text"></param>
        /// <param name="str"></param>
        /// <param name="strPosition"></param>
        /// <returns></returns>
        public static int GetIndexOfStr(string text, string str, int strPosition)
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
