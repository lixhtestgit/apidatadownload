using Newtonsoft.Json.Linq;

namespace WebApplication1.DB.Extend
{
    /// <summary>
    /// 将一个实体对象的值复制到另一个对象
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// 对象克隆,wangyunpeng.2015-2-4.本方法是wangyunpeng写的。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T Copy<T>(this T obj) where T : class
        {
            T clone = default(T);

            #region 历史版本1-BinaryFormatter在.net 5.0下存在安全漏洞已不推荐使用且报异常
            //BinaryFormatter在.net 5.0下存在安全漏洞已不推荐使用且报异常。
            //参考链接：https://docs.microsoft.com/zh-cn/dotnet/standard/serialization/binaryformatter-security-guide

            //using (MemoryStream ms = new MemoryStream(1024))
            //{
            //    BinaryFormatter binaryFormatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone));
            //    binaryFormatter.Serialize(ms, obj);
            //    ms.Seek(0, SeekOrigin.Begin);
            //    // 反序列化至另一个对象(即创建了一个原对象的深表副本)
            //    clone = (T)binaryFormatter.Deserialize(ms);
            //}
            #endregion

            #region 历史版本2-该方法依赖无参构造函数
            ////该方法依赖无参构造函数
            ////核心点2：这里的XmlSerializer序列化器要使用实例obj的类型，不能直接使用类型T，否则会报异常(当obj实例为T类型的子类时，可重现该异常)
            ////异常详细信息："ClassName":"System.InvalidOperationException","Message":"There was an error generating the XML document.","Data":null,"InnerException":{"ClassName":"System.InvalidOperationException","Message":"The type MeShopPay.View.API.Models.ConfigModel.CallBackSettingPayPal was not expected. Use the XmlInclude or SoapInclude attribute to specify types that are not known statically."
            ////核心点3: T类型中不可包含接口类型属性，如：System.Collections.Generic.IEnumerable<T>
            ////异常详细信息：Cannot serialize member 'aaa.PingPong.RiskInfoModel.Goods' of type 'System.Collections.Generic.IEnumerable`1[[aaa.PingPong.GoodModel, aaa, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]', see inner exception for more details.
            //Type objType = obj.GetType();
            //XmlSerializer tXmlSerializer = new XmlSerializer(objType);
            //using (MemoryStream ms = new MemoryStream(1024))
            //{
            //    tXmlSerializer.Serialize(ms, obj);
            //    ms.Seek(0, SeekOrigin.Begin);
            //    // 反序列化至另一个对象(即创建了一个原对象的深表副本)
            //    clone = (T)tXmlSerializer.Deserialize(ms);
            //}
            #endregion

            clone = (T)JObject.FromObject(obj).ToObject(obj.GetType());

            return clone;
        }
        /// <summary>
        /// 对象克隆,wangyunpeng.2015-2-4.本方法是wangyunpeng写的。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T Clone<T>(this T obj) where T : class
        {
            return Copy<T>(obj);
        }
    }
}
