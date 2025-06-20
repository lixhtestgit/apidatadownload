using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace WebApplication1.Helper
{
    /// <summary>
    /// Json帮助类
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// yyyy-MM-dd HH:mm:ss时间格式
        /// </summary>
        public static readonly IsoDateTimeConverter TimeFormat = new IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" };
        /// <summary>
        /// 对象转Json字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ConvertJsonToStr(object obj)
        {
            string result = string.Empty;
            if (obj == null)
            {
                return result;
            }
            try
            {
                result = JsonConvert.SerializeObject(obj);
            }
            catch (JsonReaderException ex1)
            {
                Debug.WriteLine(ex1);
                throw;
            }
            catch (JsonWriterException ex2)
            {
                Debug.WriteLine(ex2);
                throw;
            }
            catch (Exception ex3)
            {
                Debug.WriteLine(ex3);
                throw;
            }
            return result;
        }
        public static string ConvertJsonToStr(object obj, IsoDateTimeConverter isoDateTimeConverter)
        {
            string result = string.Empty;
            if (obj == null)
            {
                return result;
            }
            try
            {
                result = JsonConvert.SerializeObject(obj, isoDateTimeConverter);
            }
            catch (JsonReaderException ex1)
            {
                Debug.WriteLine(ex1);
                throw;
            }
            catch (JsonWriterException ex2)
            {
                Debug.WriteLine(ex2);
                throw;
            }
            catch (Exception ex3)
            {
                Debug.WriteLine(ex3);
                throw;
            }
            return result;
        }

        public static string ConvertJsonToStr(object obj, JsonSerializerSettings jsonSerializerSettings)
        {
            string result = string.Empty;
            if (obj == null)
            {
                return result;
            }
            try
            {
                result = JsonConvert.SerializeObject(obj, jsonSerializerSettings);
            }
            catch (JsonReaderException ex1)
            {
                Debug.WriteLine(ex1);
                throw;
            }
            catch (JsonWriterException ex2)
            {
                Debug.WriteLine(ex2);
                throw;
            }
            catch (Exception ex3)
            {
                Debug.WriteLine(ex3);
                throw;
            }
            return result;
        }
        /// <summary>
        /// 对象转Json字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="formatting"></param>
        /// <param name="converters"></param>
        /// <returns></returns>
        public static string ConvertJsonToStr(object obj, params JsonConverter[] converters)
        {
            string result = string.Empty;
            if (obj == null)
            {
                return result;
            }
            try
            {
                result = JsonConvert.SerializeObject(obj, converters);
            }
            catch (JsonReaderException ex1)
            {
                Debug.WriteLine(ex1);
                throw;
            }
            catch (JsonWriterException ex2)
            {
                Debug.WriteLine(ex2);
                throw;
            }
            catch (Exception ex3)
            {
                Debug.WriteLine(ex3);
                throw;
            }
            return result;
        }

        /// <summary>
        /// Json字符串转对象，该方法仅在反射时调用。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <returns></returns>
        public static T? ConvertStrToJsonT<T>(string str)
        {
            return ConvertStrToJson<T>(str);
        }
        /// <summary>
        /// Json字符串转对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <returns></returns>
        public static T? ConvertStrToJson<T>(string str)
        {
            return ConvertStrToJson<T>(str, new JsonConverter[0]);
        }
        /// <summary>
        /// Json字符串转对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <param name="converters"></param>
        /// <returns></returns>
        public static T? ConvertStrToJson<T>(string str, params JsonConverter[] converters)
        {
            T? @object = default;
            if (string.IsNullOrWhiteSpace(str))
            {
                return @object;
            }
            try
            {
                @object = JsonConvert.DeserializeObject<T>(str, converters);
            }
            catch (JsonReaderException ex1)
            {
                Debug.WriteLine(ex1);
                throw;
            }
            catch (JsonWriterException ex2)
            {
                Debug.WriteLine(ex2);
                throw;
            }
            catch (Exception ex3)
            {
                Debug.WriteLine(ex3);
                throw;
            }
            return @object;
        }
        /// <summary>
        /// Json字符串转对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <param name="setting"></param>
        /// <returns></returns>
        public static T? ConvertStrToJson<T>(string str, JsonSerializerSettings setting)
        {
            T? @object = default;
            if (string.IsNullOrWhiteSpace(str))
            {
                return @object;
            }
            try
            {
                @object = JsonConvert.DeserializeObject<T>(str, setting);
            }
            catch (JsonReaderException ex1)
            {
                Debug.WriteLine(ex1);
                throw;
            }
            catch (JsonWriterException ex2)
            {
                Debug.WriteLine(ex2);
                throw;
            }
            catch (Exception ex3)
            {
                Debug.WriteLine(ex3);
                throw;
            }
            return @object;
        }
        /// <summary>
        /// Json字符串转对象
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static JObject ConvertStrToJson(string str)
        {
            JObject jobj = null;
            if (string.IsNullOrWhiteSpace(str))
            {
                return jobj;
            }
            try
            {
                jobj = JObject.Parse(str);
            }
            catch (JsonReaderException ex1)
            {
                Debug.WriteLine(ex1);
                throw;
            }
            catch (JsonWriterException ex2)
            {
                Debug.WriteLine(ex2);
                throw;
            }
            catch (Exception ex3)
            {
                Debug.WriteLine(ex3);
                throw;
            }
            return jobj;
        }
        /// <summary>
        /// 将Json字符串转换为指定类型的对象
        /// </summary>
        /// <param name="str"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object ConvertStrToObject(string str)
        {
            return JsonConvert.DeserializeObject(str);
        }
        /// <summary>
        /// 将Json字符串转换为指定类型的对象
        /// </summary>
        /// <param name="str"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object ConvertStrToJson(string str, string type)
        {
            Type? typeType = Type.GetType(type);
            if (typeType == null)
            {
                return null;
            }
            return ConvertStrToJson(str, typeType);
        }
        /// <summary>
        /// 将Json字符串转换为指定类型的对象
        /// </summary>
        /// <param name="str"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object ConvertStrToJson(string str, Type type)
        {
            return ConvertStrToJson(str, type, new JsonConverter[0]);
        }
        /// <summary>
        /// 将Json字符串转换为指定类型的对象
        /// </summary>
        /// <param name="str"></param>
        /// <param name="type"></param>
        /// <param name="jsonConverts"></param>
        /// <returns></returns>
        public static object ConvertStrToJson(string str, Type type, params JsonConverter[] jsonConverts)
        {
            return JsonConvert.DeserializeObject(str, type, jsonConverts);
        }
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <param name="anonymousTypeObject"></param>
        /// <returns></returns>
        public static T? ConvertStrToT<T>(string str, T anonymousTypeObject)
        {
            return JsonConvert.DeserializeAnonymousType(str, anonymousTypeObject);
        }
        /// <summary>
        /// 将对象序列化到文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="obj"></param>
        public static void SerializerToFile(string filePath, object obj)
        {
            if (string.IsNullOrEmpty(filePath) || obj == null)
            {
                return;
            }
            string content = ConvertJsonToStr(obj);
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            File.WriteAllText(filePath, content, Encoding.UTF8);
        }
        /// <summary>
        /// 将文件反序列化到对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static T? DeserializeFromFile<T>(string filePath)
        {
            T? result = default;
            if (string.IsNullOrEmpty(filePath))
            {
                return result;
            }
            if (!File.Exists(filePath))
            {
                return result;
            }
            string content = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(content))
            {
                return result;
            }
            return ConvertStrToJson<T>(content);
        }
        /// <summary>
        /// 通过JSON（属性/值）格式的字符串设置对象的属性值
        /// </summary>
        /// <param name="srcObject">要设置的原对象</param>
        /// <param name="newPropertyValueJson">JSON（属性/值）字符串</param>
        public static void ObjectSetPropertyValue(object srcObject, string newPropertyValueJson)
        {
            if (srcObject == null || string.IsNullOrWhiteSpace(newPropertyValueJson))
            {
                return;
            }
            JObject jObject = ConvertStrToJson<JObject>(newPropertyValueJson);
            if (jObject == null)
            {
                return;
            }
            PropertyInfo[] propertyInfos = srcObject.GetType().GetProperties();
            IEnumerable<JProperty> properties = jObject.Properties();
            if (propertyInfos != null && properties != null)
            {
                foreach (JProperty jProperty in properties)
                {
                    foreach (PropertyInfo propertyInfo in propertyInfos)
                    {
                        if (string.Compare(propertyInfo.Name, jProperty.Name, true) == 0)
                        {
                            if (jProperty.Value is JValue)
                            {
                                JValue jValue = jProperty.Value as JValue;
                                object objValue = jValue?.Value;
                                if (objValue != null)
                                {
                                    Type propertyType = propertyInfo.PropertyType;
                                    TypeConverter typeConverter = TypeDescriptor.GetConverter(propertyType);
                                    try
                                    {
                                        object value = typeConverter.ConvertFromString(objValue?.ToString() ?? "");
                                        propertyInfo.SetValue(srcObject, value, null);
                                    }
                                    catch (NotSupportedException)
                                    {
                                    }
                                    catch (ArgumentException)
                                    {
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }
    }
}
