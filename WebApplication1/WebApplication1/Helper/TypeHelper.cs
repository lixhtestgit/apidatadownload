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

    }
}
