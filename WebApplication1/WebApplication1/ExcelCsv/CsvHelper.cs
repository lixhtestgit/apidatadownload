using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System;
using System.Linq;

namespace WebApplication1.ExcelCsv
{
    public class CsvHelper
    {
        /// <summary>
        /// 日志
        /// </summary>
        private ILogger _Logger { get; set; }

        public CsvHelper(ILogger<CsvHelper> logger)
        {
            this._Logger = logger;
        }

        public List<T> Read<T>(string filePath, CsvFileDescription fileDescription) where T : class, new()
        {
            List<T> tList = new List<T>(50 * 10000);

            T t = null;
            int currentRawIndex = 0;

            if (File.Exists(filePath))
            {
                using (StreamReader streamReader = new StreamReader(filePath, fileDescription.Encoding))
                {
                    Dictionary<int, CsvFieldMapper> CsvFieldMapperDic = CsvFieldMapper.GetModelFieldMapper<T>().ToDictionary(m => m.CSVTitleIndex);
                    string rawValue = null;
                    string[] rawValueArray = null;
                    PropertyInfo propertyInfo = null;
                    string propertyValue = null;
                    bool rawReadEnd = false;

                    bool isExistSplitChart = false;
                    do
                    {
                        rawValue = streamReader.ReadLine();

                        //标题行
                        if (currentRawIndex > fileDescription.TitleRawIndex)
                        {
                            if (!string.IsNullOrEmpty(rawValue))
                            {
                                //替换字符串含有分隔符为{分隔符}，最后再替换回来
                                if (rawValue.Contains("\""))
                                {
                                    isExistSplitChart = true;

                                    int yhBeginIndex = 0;
                                    int yhEndIndex = 0;
                                    string yhText = null;
                                    do
                                    {
                                        yhBeginIndex = rawValue.GetIndexOfStr("\"", 1);
                                        yhEndIndex = rawValue.GetIndexOfStr("\"", 2);
                                        yhText = rawValue.Substring(yhBeginIndex, (yhEndIndex - yhBeginIndex + 1));
                                        string newYHText = yhText.Replace("\"", "").Replace(fileDescription.SeparatorChar.ToString(), "{分隔符}");
                                        rawValue = rawValue.Replace(yhText, newYHText);
                                    } while (rawValue.Contains("\""));
                                }

                                rawValueArray = rawValue.Split(fileDescription.SeparatorChar);

                                t = new T();
                                bool isExistException = false;
                                foreach (var CsvFieldMapper in CsvFieldMapperDic)
                                {
                                    try
                                    {
                                        propertyInfo = CsvFieldMapper.Value.PropertyInfo;
                                        propertyValue = rawValueArray[CsvFieldMapper.Key - 1];
                                        if (!string.IsNullOrEmpty(propertyValue))
                                        {
                                            if (isExistSplitChart && propertyValue.Contains("{分隔符}"))
                                            {
                                                propertyValue = propertyValue.Replace("{分隔符}", fileDescription.SeparatorChar.ToString());
                                            }

                                            if (CsvFieldMapper.Value.IsCheckContentEmpty && propertyValue.IsNullOrEmpty())
                                            {
                                                t = null;
                                                break;
                                            }
                                            else
                                            {
                                                TypeHelper.SetPropertyValue(t, propertyInfo.Name, propertyValue);
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        isExistException = true;
                                        this._Logger.LogWarning(e, $"第{currentRawIndex}行数据{propertyValue}转换属性{propertyInfo.Name}-{propertyInfo.PropertyType.Name}失败！文件路径：{filePath}");
                                        break;
                                    }
                                }
                                if (isExistException == false && t != null)
                                {
                                    tList.Add(t);
                                }
                            }
                            else
                            {
                                rawReadEnd = true;
                            }
                        }
                        currentRawIndex++;
                    } while (rawReadEnd == false);
                }
            }


            return tList;
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="tList"></param>
        /// <param name="fileDescription"></param>
        public void WriteFile<T>(string path, List<T> tList, CsvFileDescription fileDescription) where T : class, new()
        {
            if (!string.IsNullOrEmpty(path))
            {
                string fileDirectoryPath = null;
                if (path.Contains("\\"))
                {
                    fileDirectoryPath = path.Substring(0, path.LastIndexOf('\\'));
                }
                else
                {
                    fileDirectoryPath = path.Substring(0, path.LastIndexOf('/'));
                }
                if (!Directory.Exists(fileDirectoryPath))
                {
                    Directory.CreateDirectory(fileDirectoryPath);
                }

                int dataCount = tList.Count;
                Dictionary<int, CsvFieldMapper> fieldMapperDic = CsvFieldMapper.GetModelFieldMapper<T>().ToDictionary(m => m.CSVTitleIndex);
                int titleCount = fieldMapperDic.Keys.Max();
                string[] rawValueArray = new string[titleCount];
                StringBuilder rawValueBuilder = new StringBuilder();
                string rawValue = null;
                T t = null;
                PropertyInfo propertyInfo = null;
                int currentRawIndex = 1;
                int tIndex = 0;

                using (StreamWriter streamWriter = new StreamWriter(path, false, fileDescription.Encoding))
                {
                    do
                    {
                        try
                        {
                            rawValue = "";

#if DEBUG
                            if (currentRawIndex % 10000 == 0)
                            {
                                this._Logger.LogInformation($"已写入文件：{path}，数据量：{currentRawIndex}");
                            }
#endif

                            if (currentRawIndex >= fileDescription.TitleRawIndex)
                            {
                                //清空数组数据
                                for (int i = 0; i < titleCount; i++)
                                {
                                    rawValueArray[i] = "";
                                }

                                if (currentRawIndex > fileDescription.TitleRawIndex)
                                {
                                    t = tList[tIndex];
                                    tIndex++;
                                }
                                foreach (var fieldMapperItem in fieldMapperDic)
                                {
                                    //写入标题行
                                    if (currentRawIndex == fileDescription.TitleRawIndex)
                                    {
                                        rawValueArray[fieldMapperItem.Key - 1] = fieldMapperItem.Value.CSVTitle;
                                    }
                                    //真正的数据从标题行下一行开始写
                                    else
                                    {
                                        propertyInfo = fieldMapperItem.Value.PropertyInfo;
                                        object propertyValue = propertyInfo.GetValue(t);
                                        string formatValue = null;
                                        if (propertyValue != null)
                                        {
                                            if (propertyInfo.PropertyType is IFormattable && !string.IsNullOrEmpty(fieldMapperItem.Value.OutputFormat))
                                            {
                                                formatValue = ((IFormattable)propertyValue).ToString(fieldMapperItem.Value.OutputFormat, null);
                                            }
                                            else
                                            {
                                                formatValue = propertyValue.ToString();
                                            }

                                            //如果属性值含有分隔符，则使用双引号包裹
                                            if (formatValue.Contains(fileDescription.SeparatorChar.ToString()))
                                            {
                                                formatValue = $"\"{formatValue}\"";
                                            }
                                            rawValueArray[fieldMapperItem.Key - 1] = formatValue;
                                        }
                                    }
                                }
                                rawValue = string.Join(fileDescription.SeparatorChar, rawValueArray);
                            }
                            rawValueBuilder.Append(rawValue + "\r\n");
                        }
                        catch (Exception e)
                        {
                            this._Logger.LogWarning(e, $"(异常)Excel第{currentRawIndex}行，数据列表第{tIndex + 1}个数据写入失败！rawValue：{rawValue}");
                            throw;
                        }

                        currentRawIndex++;
                    } while (tIndex < dataCount);
                    streamWriter.Write(rawValueBuilder.ToString());

                    streamWriter.Close();
                    streamWriter.Dispose();
                }
            }
        }

        /// <summary>
        /// 写入文件(JObject对象使用)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="tList"></param>
        /// <param name="fileDescription"></param>
        public void WriteFileForJObject<T, TCollection>(string path, TCollection tList, Dictionary<string, string> fieldNameAndShowNameDic, CsvFileDescription fileDescription) where TCollection : List<T> where T : new()
        {
            if (!string.IsNullOrEmpty(path))
            {
                string fileDirectoryPath = null;
                if (path.Contains("\\"))
                {
                    fileDirectoryPath = path.Substring(0, path.LastIndexOf('\\'));
                }
                else
                {
                    fileDirectoryPath = path.Substring(0, path.LastIndexOf('/'));
                }
                if (!Directory.Exists(fileDirectoryPath))
                {
                    Directory.CreateDirectory(fileDirectoryPath);
                }

                int dataCount = tList.Count;
                int titleCount = fieldNameAndShowNameDic.Count;
                string[] rawValueArray = new string[titleCount];
                StringBuilder rawValueBuilder = new StringBuilder();
                string rawValue = null;
                T t = default(T);
                JProperty propertyInfo = null;
                int currentRawIndex = 1;
                int tIndex = 0;

                Dictionary<int, JProperty> indexPropertyDic = this.GetIndexPropertyDicFromJObject(tList.FirstOrDefault(), fieldNameAndShowNameDic.Keys.ToList());

                using (StreamWriter streamWriter = new StreamWriter(path, false, fileDescription.Encoding))
                {
                    do
                    {
                        try
                        {
                            rawValue = "";

#if DEBUG
                            if (currentRawIndex % 10000 == 0)
                            {
                                this._Logger.LogInformation($"已写入文件：{path}，数据量：{currentRawIndex}");
                            }
#endif

                            if (currentRawIndex >= fileDescription.TitleRawIndex)
                            {
                                //清空数组数据
                                for (int i = 0; i < titleCount; i++)
                                {
                                    rawValueArray[i] = "";
                                }

                                if (currentRawIndex > fileDescription.TitleRawIndex)
                                {
                                    t = tList[tIndex];
                                    tIndex++;
                                }

                                int fieldIndex = -1;
                                foreach (var fieldMapperItem in fieldNameAndShowNameDic)
                                {
                                    fieldIndex++;
                                    //写入标题行
                                    if (currentRawIndex == fileDescription.TitleRawIndex)
                                    {
                                        rawValueArray[fieldIndex] = fieldMapperItem.Value;
                                    }
                                    //真正的数据从标题行下一行开始写
                                    else
                                    {
                                        propertyInfo = indexPropertyDic[fieldIndex];
                                        object propertyValue = JObject.FromObject(t).SelectToken(fieldMapperItem.Key);
                                        string formatValue = null;
                                        if (propertyValue != null)
                                        {
                                            formatValue = propertyValue.ToString();

                                            //如果属性值含有分隔符，则使用双引号包裹
                                            if (formatValue.Contains(fileDescription.SeparatorChar.ToString()))
                                            {
                                                formatValue = $"\"{formatValue}\"";
                                            }
                                            rawValueArray[fieldIndex] = formatValue;
                                        }
                                    }
                                }
                                rawValue = string.Join(fileDescription.SeparatorChar, rawValueArray);
                            }
                            rawValueBuilder.Append(rawValue + "\r\n");
                        }
                        catch (Exception e)
                        {
                            this._Logger.LogWarning(e, $"(异常)Excel第{currentRawIndex}行，数据列表第{tIndex + 1}个数据写入失败！rawValue：{rawValue}");
                            throw;
                        }

                        currentRawIndex++;
                    } while (tIndex < dataCount);
                    streamWriter.Write(rawValueBuilder.ToString());

                    streamWriter.Close();
                    streamWriter.Dispose();
                }
            }
        }

        /// <summary>
        /// 根据属性名顺序获取对应的属性对象
        /// </summary>
        /// <param name="fieldNameList"></param>
        /// <returns></returns>
        private Dictionary<int, JProperty> GetIndexPropertyDicFromJObject<T>(T t, List<string> fieldNameList)
        {
            Dictionary<int, JProperty> indexPropertyDic = new Dictionary<int, JProperty>(fieldNameList.Count);
            JObject jObj = JObject.FromObject(t);
            List<JProperty> tPropertyInfoList = jObj.Properties().ToList();
            JProperty propertyInfo = null;
            for (int i = 0; i < fieldNameList.Count; i++)
            {
                propertyInfo = tPropertyInfoList.Find(m => m.Name.Equals(fieldNameList[i], StringComparison.OrdinalIgnoreCase));
                indexPropertyDic.Add(i, propertyInfo);
            }

            return indexPropertyDic;
        }
    }
}
