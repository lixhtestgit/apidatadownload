using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WebApplication1.Extension
{
    /// <summary>
    /// 列表扩展类
    /// </summary>
    public static class ListExtend
    {
        /// <summary>
        /// 相同属性类，不同属性类型的类型转换
        /// </summary>
        /// <typeparam name="Tout"></typeparam>
        /// <typeparam name="Tin"></typeparam>
        /// <param name="tinList"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        public static List<Tout> ConvertListPropertyType<Tout, Tin>(this List<Tin> tinList, out string remark) where Tin : new() where Tout : new()
        {
            List<Tout> t1List = new List<Tout>(0);
            remark = "";

            Dictionary<string, PropertyInfo> t1TypeDic = typeof(Tout).GetProperties().ToDictionary(m => m.Name);
            Dictionary<string, PropertyInfo> t2TypeDic = typeof(Tin).GetProperties().ToDictionary(m => m.Name);

            Tout t1 = default;
            object pValue = null;
            PropertyInfo t1PropertyInfo = null;
            foreach (var t2 in tinList)
            {
                t1 = new Tout();
                foreach (var item in t2TypeDic)
                {
                    pValue = item.Value.GetValue(t2);
                    if (t1TypeDic.ContainsKey(item.Key))
                    {
                        t1PropertyInfo = t1TypeDic[item.Key];
                        try
                        {
                            if (t1PropertyInfo.PropertyType == typeof(int))
                            {
                                t1PropertyInfo.SetValue(t1, Convert.ToInt32(pValue));
                            }
                            else if (t1PropertyInfo.PropertyType == typeof(decimal))
                            {
                                t1PropertyInfo.SetValue(t1, Convert.ToDecimal(pValue));
                            }
                            else if (t1PropertyInfo.PropertyType == typeof(bool))
                            {
                                t1PropertyInfo.SetValue(t1, Convert.ToBoolean(pValue));
                            }
                            else if (t1PropertyInfo.PropertyType == typeof(DateTime))
                            {
                                t1PropertyInfo.SetValue(t1, Convert.ToDateTime(pValue));
                            }
                            else if (t1PropertyInfo.PropertyType.IsEnum)
                            {
                                t1PropertyInfo.SetValue(t1, System.Enum.Parse(t1PropertyInfo.PropertyType, pValue.ToString()));
                            }
                            else
                            {
                                t1PropertyInfo.SetValue(t1, pValue);
                            }
                        }
                        catch (Exception)
                        {
                            remark += "原数据：" + JsonConvert.SerializeObject(t2) + "转换属性：" + t1PropertyInfo.Name + "_" + t1PropertyInfo.PropertyType.FullName + "失败!\r\n";
                            break;
                        }

                    }
                }
                t1List.Add(t1);
            }

            return t1List;
        }
    }
}
