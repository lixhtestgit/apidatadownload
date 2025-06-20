using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace WebApplication1.Helper
{
    /// <summary>
    /// 类型转换
    /// </summary>
    public static class TypeParseHelper
    {
        /// <summary>
        /// 将字符串转换成Int32数据类型
        /// </summary>
        /// <param name="str">要转换的字符串</param>
        /// <returns></returns>
        public static int StrToInt32(string str)
        {
            int result = 0;
            if (!string.IsNullOrWhiteSpace(str))
            {
                Int32.TryParse(str, out result);
            }
            return result;
        }
        /// <summary>
        /// 将对象转换成Int32数据类型
        /// </summary>
        /// <param name="obj">要转换的对象</param>
        /// <returns></returns>
        public static int StrToInt32(object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            return StrToInt32(obj.ToString());
        }
        /// <summary>
        /// 将字符串转换成UInt32数据类型
        /// </summary>
        /// <param name="str">要转换的字符串</param>
        /// <returns></returns>
        public static uint StrToUInt32(string str)
        {
            uint result = 0;
            if (!string.IsNullOrWhiteSpace(str))
            {
                UInt32.TryParse(str, out result);
            }
            return result;
        }
        /// <summary>
        /// 将对象转换成UInt32数据类型
        /// </summary>
        /// <param name="obj">要转换的对象</param>
        /// <returns></returns>
        public static uint StrToUInt32(object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            return StrToUInt32(obj.ToString());
        }
        /// <summary>
        /// 将字符串转换成Int64数据类型
        /// </summary>
        /// <param name="str">要转换的字符串</param>
        /// <returns></returns>
        public static long StrToInt64(string str)
        {
            long result = 0L;
            if (!string.IsNullOrWhiteSpace(str))
            {
                Int64.TryParse(str, out result);
            }
            return result;
        }
        /// <summary>
        /// 将对象转换成Int64数据类型
        /// </summary>
        /// <param name="str">要转换的对象</param>
        /// <returns></returns>
        public static long StrToInt64(object obj)
        {
            if (obj == null)
            {
                return 0L;
            }
            return StrToInt64(obj.ToString());
        }
        /// <summary>
        /// 将字符串转换成Double数据类型
        /// </summary>
        /// <param name="str">要转换的字符串</param>
        /// <returns></returns>
        public static double StrToDouble(string str)
        {
            double result = 0.0;
            if (!string.IsNullOrWhiteSpace(str))
            {
                Double.TryParse(str, out result);
            }
            return result;
        }
        /// <summary>
        /// 将对象转换成Double数据类型
        /// </summary>
        /// <param name="obj">要转换的对象</param>
        /// <returns></returns>
        public static double StrToDouble(object obj)
        {
            if (obj == null)
            {
                return 0.0;
            }
            return StrToDouble(obj.ToString());
        }
        /// <summary>
        /// 将字符串转换成Float数据类型
        /// </summary>
        /// <param name="str">要转换的字符串</param>
        /// <returns></returns>
        public static float StrToFloat(string str)
        {
            float result = 0.0f;
            if (!string.IsNullOrWhiteSpace(str))
            {
                float.TryParse(str, out result);
            }
            return result;
        }
        /// <summary>
        /// 将对象转换成Float数据类型
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static float StrToFloat(object obj)
        {
            if (obj == null)
            {
                return 0.0f;
            }
            return StrToFloat(obj.ToString());
        }
        /// <summary>
        /// 将字符串转换成DateTime数据类型
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static DateTime StrToDateTime(string str)
        {
            DateTime result = DateTime.MinValue;
            if (!string.IsNullOrWhiteSpace(str))
            {
                DateTime.TryParse(str, out result);
            }
            return result;
        }
        /// <summary>
        /// 将字符串转换成DateTime数据类型
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static DateTime StrToDateTime(object obj)
        {
            if (obj == null)
            {
                return DateTime.MinValue;
            }
            return StrToDateTime(obj.ToString());
        }
        /// <summary>
        /// 将字符串转换成Boolean数据类型
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool StrToBoolean(string str)
        {
            bool result = false;
            if (!string.IsNullOrWhiteSpace(str))
            {
                Boolean.TryParse(str, out result);
            }
            return result;
        }
        /// <summary>
        /// 将字符串转换成Boolean数据类型
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool StrToBoolean(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            return StrToBoolean(obj.ToString());
        }
        /// <summary>
        /// 将字符串转换成Decimal数据类型
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static decimal StrToDecimal(string str)
        {
            decimal result = 0.0M;
            if (!string.IsNullOrWhiteSpace(str))
            {
                Decimal.TryParse(str, out result);
            }
            return result;
        }
        /// <summary>
        /// 将字符串转换成Decimal数据类型
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static decimal StrToDecimal(object obj)
        {
            if (obj == null)
            {
                return 0.0M;
            }
            return StrToDecimal(obj.ToString());
        }

        public static string TickToDate(long tick)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(tick);
            return string.Format("{0}时 {1}分 {2}秒 {3} 毫秒", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
        }

        /// <summary>
        /// 将字符串转换成Guid数据类型
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Guid StrToGuid(string str)
        {
            Guid result = Guid.Empty;
            if (!string.IsNullOrWhiteSpace(str))
            {
                Guid.TryParse(str, out result);
            }
            return result;
        }
        /// <summary>
        /// 将字符串转换成Guid数据类型
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Guid StrToGuid(object obj)
        {
            if (obj == null)
            {
                return Guid.Empty;
            }
            return StrToGuid(obj.ToString());
        }
        /// <summary>
        /// string数组转int32数组
        /// </summary>
        /// <param name="strings"></param>
        /// <returns></returns>
        public static int[] StrsToInt32s(string[] strings)
        {
            int[] ints = null;
            if (strings != null)
            {
                int length = strings.Length;
                ints = new int[length];
                for (int i = 0; i < length; i++)
                {
                    ints[i] = StrToInt32(strings[i]);
                }
            }
            return ints;
        }
        /// <summary>
        /// string数组转Int64数组
        /// </summary>
        /// <param name="strings"></param>
        /// <returns></returns>
        public static long[] StrsToInt64s(string[] strings)
        {
            long[] ints = null;
            if (strings != null)
            {
                int length = strings.Length;
                ints = new long[length];
                for (int i = 0; i < length; i++)
                {
                    ints[i] = StrToInt64(strings[i]);
                }
            }
            return ints;
        }
        /// <summary>
        /// 将字符串转换成T数据类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <returns></returns>
        public static T StrToT<T>(string str)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(str, typeof(T));
            }
            object[] parameters = new object[] { str, default(T) };
            Type type = typeof(T);
            MethodInfo methodInfo = type.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string), type.MakeByRefType() }, null);
            object result = methodInfo.Invoke(null, BindingFlags.Static | BindingFlags.Public, null, parameters, null);
            if ((bool)result)
            {
                return (T)parameters[1];
            }
            return default;
        }
        /// <summary>
        /// 将值转换为指定类型的值。
        /// 从MVC里面取出来的源代码。
        /// </summary>
        /// <param name="destinationType">目标类型</param>
        /// <param name="value">被转换的值</param>
        /// <returns>转换之后的值</returns>
        public static object ConvertTo(Type destinationType, object value)
        {
            if (value == null || destinationType.IsInstanceOfType(value))
            {
                return value;
            }

            // array conversion results in four cases, as below
            Array valueAsArray = value as Array;
            if (destinationType.IsArray)
            {
                Type destinationElementType = destinationType.GetElementType();
                if (valueAsArray != null)
                {
                    // case 1: both destination + source type are arrays, so convert each element
                    IList converted = Array.CreateInstance(destinationElementType, valueAsArray.Length);
                    for (int i = 0; i < valueAsArray.Length; i++)
                    {
                        converted[i] = ConvertSimpleType(valueAsArray.GetValue(i), destinationElementType);
                    }
                    return converted;
                }
                else
                {
                    // case 2: destination type is array but source is single element, so wrap element in array + convert
                    object element = ConvertSimpleType(value, destinationElementType);
                    IList converted = Array.CreateInstance(destinationElementType, 1);
                    converted[0] = element;
                    return converted;
                }
            }
            else if (valueAsArray != null)
            {
                // case 3: destination type is single element but source is array, so extract first element + convert
                if (valueAsArray.Length > 0)
                {
                    value = valueAsArray.GetValue(0);
                    return ConvertSimpleType(value, destinationType);
                }
                else
                {
                    // case 3(a): source is empty array, so can't perform conversion
                    return null;
                }
            }
            // case 4: both destination + source type are single elements, so convert
            return ConvertSimpleType(value, destinationType);
        }
        private static object ConvertSimpleType(object value, Type destinationType)
        {
            CultureInfo culture = CultureInfo.CurrentUICulture;
            if (value == null || destinationType.IsInstanceOfType(value))
            {
                return value;
            }

            // if this is a user-input value but the user didn't type anything, return no value
            string valueAsString = value as string;
            if (valueAsString != null && valueAsString.Length == 0)
            {
                return null;
            }

            TypeConverter converter = TypeDescriptor.GetConverter(destinationType);
            bool canConvertFrom = converter.CanConvertFrom(value.GetType());
            if (!canConvertFrom)
            {
                converter = TypeDescriptor.GetConverter(value.GetType());
            }
            if (!(canConvertFrom || converter.CanConvertTo(destinationType)))
            {
                string message = String.Format(CultureInfo.CurrentUICulture, "NoConverterExists", value.GetType().FullName, destinationType.FullName);
                throw new InvalidOperationException(message);
            }
            object convertedValue = null;
            try
            {
                convertedValue = canConvertFrom ? converter.ConvertFrom(null /* context */, culture, value) : converter.ConvertTo(null /* context */, culture, value, destinationType);
            }
            catch (Exception ex)
            {
                string message = String.Format(CultureInfo.CurrentUICulture, "ConversionThrew", value.GetType().FullName, destinationType.FullName);
                throw new InvalidOperationException(message, ex);
            }
            return convertedValue;
        }
        /// <summary>
        /// 转换十六进制数
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Str2Hex(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }
            System.Text.StringBuilder sb = new System.Text.StringBuilder("");
            char[] src = text.ToCharArray();
            for (int i = 0; i < src.Length; i++)
            {
                byte[] bytes = System.Text.Encoding.Unicode.GetBytes(src[i].ToString());
                sb.Append(string.Format(@"\u{0}{1}", bytes[1].ToString("X2"), bytes[0].ToString("X2")));
            }
            return sb.ToString();
        }
        /// <summary>
        /// 将长整型数字变成字符串
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Pack(long value)
        {
            ulong a = (ulong)value; // make shift easy
            List<byte> bytes = new List<byte>(8);
            while (a != 0)
            {
                bytes.Add((byte)a);
                a >>= 8;
            }
            bytes.Reverse();
            var chunk = bytes.ToArray();
            return Convert.ToBase64String(chunk);
        }
        /// <summary>
        /// 将字符串变成长整型数字
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static long Unpack(string value)
        {
            var chunk = Convert.FromBase64String(value);
            ulong a = 0;
            for (int i = 0; i < chunk.Length; i++)
            {
                a <<= 8;
                a |= chunk[i];
            }
            return (long)a;
        }
    }

}
