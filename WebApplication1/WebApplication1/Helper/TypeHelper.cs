using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication1
{
    public class TypeHelper
    {
        private static Dictionary<Type, List<PropertyInfo>> _TypePropertyDic = new Dictionary<Type, List<PropertyInfo>>(0);

        /// <summary>
        /// 获取T属性列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<PropertyInfo> GetTPropertyDic<T>()
        {
            Type tType = typeof(T);
            List<PropertyInfo> propertyInfoList = null;
            if (TypeHelper._TypePropertyDic.ContainsKey(tType))
            {
                propertyInfoList = TypeHelper._TypePropertyDic[tType];
            }
            else
            {
                propertyInfoList = tType.GetProperties().ToList();
                TypeHelper._TypePropertyDic.Add(tType, propertyInfoList);
            }

            return propertyInfoList;
        }

        public static object GetPropertyValue<T>(T t, string propertyName)
        {
            object value = null;

            Type tType = typeof(T);
            List<PropertyInfo> propertyInfoList = null;
            if (TypeHelper._TypePropertyDic.ContainsKey(tType))
            {
                propertyInfoList = TypeHelper._TypePropertyDic[tType];
            }
            else
            {
                propertyInfoList = tType.GetProperties().ToList();
                TypeHelper._TypePropertyDic.Add(tType, propertyInfoList);
            }
            PropertyInfo propertyInfo = propertyInfoList.FirstOrDefault(m => m.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (propertyInfo != null)
            {
                value = propertyInfo.GetValue(t);
            }
            return value;
        }

        /// <summary>
        /// 设置t属性值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        public static void SetPropertyValue<T>(T t, string propertyName, object propertyValue)
        {
            Type tType = typeof(T);
            List<PropertyInfo> propertyInfoList = TypeHelper.GetTPropertyDic<T>();

            PropertyInfo propertyInfo = propertyInfoList.FirstOrDefault(m => m.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (propertyInfo != null && propertyValue != null)
            {
                if (!propertyInfo.PropertyType.IsGenericType)
                {
                    propertyInfo.SetValue(t, Convert.ChangeType(propertyValue, propertyInfo.PropertyType));
                }
                else
                {
                    Type genericTypeDefinition = propertyInfo.PropertyType.GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(Nullable<>))
                    {
                        propertyInfo.SetValue(t, Convert.ChangeType(propertyValue, Nullable.GetUnderlyingType(propertyInfo.PropertyType)));
                    }
                }
            }
        }

    }
}
