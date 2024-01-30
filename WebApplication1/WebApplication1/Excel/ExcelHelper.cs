using Microsoft.Extensions.Logging;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PPPayReportTools.Excel
{
    /// <summary>
    /// EXCEL帮助类
    /// </summary>
    public class ExcelHelper
    {
        public ILogger Logger { get; set; }
        public ExcelHelper(ILogger<ExcelHelper> logger)
        {
            this.Logger = logger;
        }

        public IWorkbook CreateWorkbook(string filePath)
        {
            IWorkbook workbook = null;

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    workbook = new XSSFWorkbook(fileStream);
                }
                catch (Exception)
                {
                    workbook = new HSSFWorkbook(fileStream);
                }
            }
            return workbook;
        }

        #region 创建工作表

        /// <summary>
        /// 将列表数据生成工作表
        /// </summary>
        /// <param name="tList">要导出的数据集</param>
        /// <param name="fieldNameAndShowNameDic">键值对集合（键：字段名，值：显示名称）</param>
        /// <param name="workbook">更新时添加：要更新的工作表</param>
        /// <param name="sheetName">指定要创建的sheet名称时添加</param>
        /// <param name="excelFileDescription">读取或插入定制需求时添加</param>
        /// <returns></returns>
        public IWorkbook CreateOrUpdateWorkbook<T>(List<T> tList, Dictionary<string, string> fieldNameAndShowNameDic, IWorkbook workbook = null, string sheetName = "sheet1", ExcelFileDescription excelFileDescription = null) where T : new()
        {
            List<ExcelTitleFieldMapper> titleMapperList = ExcelTitleFieldMapper.GetModelFieldMapper<T>(fieldNameAndShowNameDic);

            workbook = this.CreateOrUpdateWorkbook<T>(tList, titleMapperList, workbook, sheetName, excelFileDescription);
            return workbook;
        }
        /// <summary>
        /// 将列表数据生成工作表（T的属性需要添加：属性名列名映射关系）
        /// </summary>
        /// <param name="tList">要导出的数据集</param>
        /// <param name="workbook">更新时添加：要更新的工作表</param>
        /// <param name="sheetName">指定要创建的sheet名称时添加</param>
        /// <param name="excelFileDescription">读取或插入定制需求时添加</param>
        /// <returns></returns>
        public IWorkbook CreateOrUpdateWorkbook<T>(List<T> tList, IWorkbook workbook = null, string sheetName = "sheet1", ExcelFileDescription excelFileDescription = null) where T : new()
        {
            List<ExcelTitleFieldMapper> titleMapperList = ExcelTitleFieldMapper.GetModelFieldMapper<T>();

            workbook = this.CreateOrUpdateWorkbook<T>(tList, titleMapperList, workbook, sheetName, excelFileDescription);
            return workbook;
        }

        private IWorkbook CreateOrUpdateWorkbook<T>(List<T> tList, List<ExcelTitleFieldMapper> titleMapperList, IWorkbook workbook, string sheetName, ExcelFileDescription excelFileDescription = null)
        {
            //xls文件格式属于老版本文件，一个sheet最多保存65536行；而xlsx属于新版文件类型；
            //Excel 07 - 2003一个工作表最多可有65536行，行用数字1—65536表示; 最多可有256列，列用英文字母A—Z，AA—AZ，BA—BZ，……，IA—IV表示；一个工作簿中最多含有255个工作表，默认情况下是三个工作表；
            //Excel 2007及以后版本，一个工作表最多可有1048576行，16384列；
            if (workbook == null)
            {
                workbook = new XSSFWorkbook();
                //workbook = new HSSFWorkbook();
            }
            ISheet worksheet = null;
            if (workbook.GetSheetIndex(sheetName) >= 0)
            {
                worksheet = workbook.GetSheet(sheetName);
            }
            else
            {
                worksheet = workbook.CreateSheet(sheetName);
            }

            IRow row1 = null;
            ICell cell = null;

            int defaultBeginTitleIndex = 0;
            if (excelFileDescription != null)
            {
                defaultBeginTitleIndex = excelFileDescription.TitleRowIndex;
            }

            PropertyInfo propertyInfo = null;
            T t = default(T);

            int tCount = tList.Count;
            int currentRowIndex = 0;
            int dataIndex = -1;
            do
            {
                row1 = worksheet.GetRow(currentRowIndex);
                if (row1 == null)
                {
                    row1 = worksheet.CreateRow(currentRowIndex);
                }

                if (currentRowIndex >= defaultBeginTitleIndex)
                {
                    //到达标题行
                    if (currentRowIndex == defaultBeginTitleIndex)
                    {
                        int cellIndex = 0;
                        foreach (var titleMapper in titleMapperList)
                        {
                            cell = row1.GetCell(cellIndex);

                            if (cell == null)
                            {
                                cell = row1.CreateCell(cellIndex);
                            }
                            this.SetCellValue(cell, titleMapper.ExcelTitle, outputFormat: null);
                            cellIndex++;
                        }
                    }
                    //到达内容行
                    else
                    {
                        dataIndex = currentRowIndex - defaultBeginTitleIndex - 1;
                        if (dataIndex <= tCount - 1)
                        {
                            t = tList[dataIndex];

                            int cellIndex = 0;
                            foreach (var titleMapper in titleMapperList)
                            {
                                propertyInfo = titleMapper.PropertyInfo;

                                cell = row1.GetCell(cellIndex);
                                if (cell == null)
                                {
                                    cell = row1.CreateCell(cellIndex);
                                }

                                this.SetCellValue<T>(cell, t, titleMapper);

                                cellIndex++;
                            }

                            //重要：设置行宽度自适应(大批量添加数据时，该行代码需要注释，否则会极大减缓Excel添加行的速度！)
                            //worksheet.AutoSizeColumn(i, true);
                        }
                    }
                }

                currentRowIndex++;

            } while (dataIndex < tCount - 1);

            //设置表达式重算（如果不添加该代码，表达式更新不出结果值）
            worksheet.ForceFormulaRecalculation = true;

            return workbook;
        }

        /// <summary>
        /// 将单元格数据列表生成工作表
        /// </summary>
        /// <param name="commonCellList">所有的单元格数据列表</param>
        /// <param name="workbook">更新时添加：要更新的工作表</param>
        /// <param name="sheetName">指定要创建的sheet名称时添加</param>
        /// <returns></returns>
        public IWorkbook CreateOrUpdateWorkbook(CommonCellModelColl commonCellList, IWorkbook workbook = null, string sheetName = "sheet1")
        {
            //xls文件格式属于老版本文件，一个sheet最多保存65536行；而xlsx属于新版文件类型；
            //Excel 07 - 2003一个工作表最多可有65536行，行用数字1—65536表示; 最多可有256列，列用英文字母A—Z，AA—AZ，BA—BZ，……，IA—IV表示；一个工作簿中最多含有255个工作表，默认情况下是三个工作表；
            //Excel 2007及以后版本，一个工作表最多可有1048576行，16384列；
            if (workbook == null)
            {
                workbook = new XSSFWorkbook();
                //workbook = new HSSFWorkbook();
            }
            ISheet worksheet = null;
            if (workbook.GetSheetIndex(sheetName) >= 0)
            {
                worksheet = workbook.GetSheet(sheetName);
            }
            else
            {
                worksheet = workbook.CreateSheet(sheetName);
            }

            //设置首列显示
            IRow row1 = null;
            int rowIndex = 0;
            int columnIndex = 0;
            int maxColumnIndex = 0;
            Dictionary<int, CommonCellModel> rowColumnIndexCellDIC = null;
            ICell cell = null;
            object cellValue = null;

            do
            {
                rowColumnIndexCellDIC = commonCellList.GetRawCellList(rowIndex).ToDictionary(m => m.ColumnIndex);
                maxColumnIndex = rowColumnIndexCellDIC.Count > 0 ? rowColumnIndexCellDIC.Keys.Max() : 0;

                if (rowColumnIndexCellDIC != null && rowColumnIndexCellDIC.Count > 0)
                {
                    row1 = worksheet.GetRow(rowIndex);
                    if (row1 == null)
                    {
                        row1 = worksheet.CreateRow(rowIndex);
                    }
                    columnIndex = 0;
                    do
                    {
                        cell = row1.GetCell(columnIndex);
                        if (cell == null)
                        {
                            cell = row1.CreateCell(columnIndex);
                        }

                        if (rowColumnIndexCellDIC.ContainsKey(columnIndex))
                        {
                            cellValue = rowColumnIndexCellDIC[columnIndex].CellValue;

                            this.SetCellValue(cell, cellValue, outputFormat: null, rowColumnIndexCellDIC[columnIndex].IsCellFormula);
                        }
                        columnIndex++;
                    } while (columnIndex <= maxColumnIndex);
                }
                rowIndex++;
            } while (rowColumnIndexCellDIC != null && rowColumnIndexCellDIC.Count > 0);

            //设置表达式重算（如果不添加该代码，表达式更新不出结果值）
            worksheet.ForceFormulaRecalculation = true;

            return workbook;
        }

        /// <summary>
        /// 更新模板文件数据：将使用单元格映射的数据T存入模板文件中
        /// </summary>
        /// <param name="filePath">所有的单元格数据列表</param>
        /// <param name="sheetName">sheet名称</param>
        /// <param name="t">添加了单元格参数映射的数据对象</param>
        /// <returns></returns>
        public IWorkbook CreateOrUpdateWorkbook<T>(string filePath, string sheetName, T t)
        {
            IWorkbook workbook = this.CreateWorkbook(filePath);
            ISheet worksheet = this.GetSheet(workbook, sheetName);

            CommonCellModelColl commonCellColl = this._ReadCellList(worksheet);

            //获取t的单元格映射列表
            Dictionary<string, ExcelCellFieldMapper> tParamMapperDic = ExcelCellFieldMapper.GetModelFieldMapper<T>().ToDictionary(m => m.CellParamName);

            var rows = worksheet.GetRowEnumerator();
            IRow row;
            ICell cell;
            string cellValue;
            ExcelCellFieldMapper cellMapper;
            while (rows.MoveNext())
            {
                row = (XSSFRow)rows.Current;
                int cellCount = row.Cells.Count;

                for (int i = 0; i < cellCount; i++)
                {
                    cell = row.Cells[i];
                    cellValue = cell.ToString();
                    if (tParamMapperDic.ContainsKey(cellValue))
                    {
                        cellMapper = tParamMapperDic[cellValue];
                        this.SetCellValue<T>(cell, t, cellMapper);
                    }
                }

            }

            if (tParamMapperDic.Count > 0)
            {
                //循环所有单元格数据替换指定变量数据
                foreach (var cellItem in commonCellColl)
                {
                    cellValue = cellItem.CellValue.ToString();

                    if (tParamMapperDic.ContainsKey(cellValue))
                    {
                        cellItem.CellValue = tParamMapperDic[cellValue].PropertyInfo.GetValue(t);
                    }
                }
            }

            //设置表达式重算（如果不添加该代码，表达式更新不出结果值）
            worksheet.ForceFormulaRecalculation = true;

            return workbook;
        }

        #endregion

        #region 保存工作表到文件

        /// <summary>
        /// 保存Workbook数据为文件
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="fileDirectoryPath"></param>
        /// <param name="fileName"></param>
        public void SaveWorkbookToFile(IWorkbook workbook, string filePath)
        {
            //xls文件格式属于老版本文件，一个sheet最多保存65536行；而xlsx属于新版文件类型；
            //Excel 07 - 2003一个工作表最多可有65536行，行用数字1—65536表示; 最多可有256列，列用英文字母A—Z，AA—AZ，BA—BZ，……，IA—IV表示；一个工作簿中最多含有255个工作表，默认情况下是三个工作表；
            //Excel 2007及以后版本，一个工作表最多可有1048576行，16384列；

            MemoryStream ms = new MemoryStream();
            //这句代码非常重要，如果不加，会报：打开的EXCEL格式与扩展名指定的格式不一致
            ms.Seek(0, SeekOrigin.Begin);
            workbook.Write(ms);
            byte[] myByteArray = ms.GetBuffer();

            string fileDirectoryPath = filePath.Substring(0, filePath.LastIndexOf("\\") + 1);
            if (!Directory.Exists(fileDirectoryPath))
            {
                Directory.CreateDirectory(fileDirectoryPath);
            }
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            File.WriteAllBytes(filePath, myByteArray);
        }

        #endregion

        #region 获取ISheet对象

        public List<ISheet> GetSheetList(IWorkbook workbook)
        {
            int sheetCount = workbook.NumberOfSheets;
            List<ISheet> sheetList = new List<ISheet>(sheetCount);

            int currentSheetIndex = 0;
            do
            {
                var sheet = workbook.GetSheetAt(currentSheetIndex);
                sheetList.Add(sheet);
                currentSheetIndex++;
            } while ((currentSheetIndex + 1) <= sheetCount);
            return sheetList;
        }

        public List<ISheet> GetSheetList(string filePath)
        {
            IWorkbook workbook = this.CreateWorkbook(filePath);
            return this.GetSheetList(workbook);
        }

        public ISheet GetSheet(IWorkbook workbook, string sheetName = null)
        {
            List<ISheet> sheetList = this.GetSheetList(workbook);
            if (!string.IsNullOrWhiteSpace(sheetName))
            {
                return sheetList.FirstOrDefault(m => m.SheetName.Equals(sheetName, StringComparison.OrdinalIgnoreCase));
            }
            return sheetList.FirstOrDefault();
        }

        #endregion

        #region 行对象

        public IRow GetOrCreateRow(ISheet sheet, int rowIndex)
        {
            IRow row = null;
            if (sheet != null)
            {
                row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    row = sheet.CreateRow(rowIndex);
                }
            }
            return row;
        }

        #endregion

        #region 单元格对象

        public ICell GetOrCreateCell(ISheet sheet, int rowIndex, int columnIndex)
        {
            ICell cell = null;

            IRow row = this.GetOrCreateRow(sheet, rowIndex);
            if (row != null)
            {
                cell = row.GetCell(columnIndex);
                if (cell == null)
                {
                    cell = row.CreateCell(columnIndex);
                }
            }

            return cell;
        }

        #endregion

        #region 读取Excel数据

        public List<T> ReadTitleDataList<T>(ISheet sheet, ExcelFileDescription excelFileDescription) where T : new()
        {
            return this.ReadTitleDataList<T>(sheet, titleMapperList: null, excelFileDescription);
        }

        /// <summary>
        /// 读取Excel数据1_手动提供属性信息和标题对应关系
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="fieldNameAndShowNameDic"></param>
        /// <param name="excelFileDescription"></param>
        /// <returns></returns>
        public List<T> ReadTitleDataList<T>(string filePath, Dictionary<string, string> fieldNameAndShowNameDic, ExcelFileDescription excelFileDescription) where T : new()
        {
            //标题属性字典列表
            List<ExcelTitleFieldMapper> titleMapperList = ExcelTitleFieldMapper.GetModelFieldMapper<T>(fieldNameAndShowNameDic);

            List<T> tList = this._GetTList<T>(filePath, titleMapperList, excelFileDescription);
            return tList ?? new List<T>(0);
        }

        /// <summary>
        /// 读取Excel数据2_使用Excel标记特性和文件描述自动创建关系
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="excelFileDescription"></param>
        /// <returns></returns>
        public List<T> ReadTitleDataList<T>(string filePath, ExcelFileDescription excelFileDescription) where T : new()
        {
            //标题属性字典列表
            List<ExcelTitleFieldMapper> titleMapperList = ExcelTitleFieldMapper.GetModelFieldMapper<T>();

            List<T> tList = this._GetTList<T>(filePath, titleMapperList, excelFileDescription);
            return tList ?? new List<T>(0);
        }

        private List<T> _GetTList<T>(string filePath, List<ExcelTitleFieldMapper> titleMapperList, ExcelFileDescription excelFileDescription) where T : new()
        {
            List<T> tList = new List<T>(1000);
            if (!File.Exists(filePath))
            {
                return tList;
            }

            IWorkbook workbook = this.CreateWorkbook(filePath);
            List<ISheet> sheetList = this.GetSheetList(workbook);
            foreach (ISheet sheet in sheetList)
            {
                tList.AddRange(this.ReadTitleDataList<T>(sheet, titleMapperList, excelFileDescription));
            }

            return tList ?? new List<T>(0);
        }

        private List<T> ReadTitleDataList<T>(ISheet sheet, List<ExcelTitleFieldMapper> titleMapperList, ExcelFileDescription excelFileDescription) where T : new()
        {
            if (titleMapperList == null || titleMapperList.Count == 0)
            {
                titleMapperList = ExcelTitleFieldMapper.GetModelFieldMapper<T>();
            }

            List<T> tList = new List<T>(500 * 10000);
            T t = default(T);

            IWorkbook workbook = sheet.Workbook;
            IFormulaEvaluator formulaEvaluator = null;

            if (workbook is XSSFWorkbook)
            {
                formulaEvaluator = new XSSFFormulaEvaluator(workbook);
            }
            else if (workbook is HSSFWorkbook)
            {
                formulaEvaluator = new HSSFFormulaEvaluator(workbook);
            }

            //标题下标属性字典
            Dictionary<int, ExcelTitleFieldMapper> sheetTitleIndexPropertyDic = new Dictionary<int, ExcelTitleFieldMapper>(0);

            //如果没有设置标题行，则通过自动查找方法获取
            int currentSheetRowTitleIndex = 0;
            if (excelFileDescription.TitleRowIndex < 0)
            {
                string[] titleArray = titleMapperList.Select(m => m.ExcelTitle).ToArray();
                currentSheetRowTitleIndex = this.GetSheetTitleIndex(sheet, titleArray);
            }
            else
            {
                currentSheetRowTitleIndex = excelFileDescription.TitleRowIndex;
            }

            var rows = sheet.GetRowEnumerator();

            bool isHaveTitleIndex = false;
            //含有Excel行下标
            if (titleMapperList.Count > 0 && titleMapperList[0].ExcelTitleIndex >= 0)
            {
                isHaveTitleIndex = true;

                foreach (var titleMapper in titleMapperList)
                {
                    sheetTitleIndexPropertyDic.Add(titleMapper.ExcelTitleIndex, titleMapper);
                }
            }

            PropertyInfo propertyInfo = null;
            int currentRowIndex = 0;

            while (rows.MoveNext())
            {
                IRow row = (IRow)rows.Current;
                currentRowIndex = row.RowNum;

                //到达标题行
                if (isHaveTitleIndex == false && currentRowIndex == currentSheetRowTitleIndex)
                {
                    ICell cell = null;
                    string cellValue = null;
                    Dictionary<string, ExcelTitleFieldMapper> titleMapperDic = titleMapperList.ToDictionary(m => m.ExcelTitle);
                    for (int i = 0; i < row.Cells.Count; i++)
                    {
                        cell = row.Cells[i];
                        cellValue = cell.StringCellValue;
                        if (titleMapperDic.ContainsKey(cellValue))
                        {
                            sheetTitleIndexPropertyDic.Add(i, titleMapperDic[cellValue]);
                        }
                    }
                    if (sheetTitleIndexPropertyDic.Count == 0)
                    {
                        break;
                    }
                }

                //到达内容行
                if (currentRowIndex > currentSheetRowTitleIndex)
                {
                    t = new T();
                    ExcelTitleFieldMapper excelTitleFieldMapper = null;
                    foreach (var titleIndexItem in sheetTitleIndexPropertyDic)
                    {
                        ICell cell = row.GetCell(titleIndexItem.Key);

                        excelTitleFieldMapper = titleIndexItem.Value;

                        //没有数据的单元格默认为null
                        propertyInfo = excelTitleFieldMapper.PropertyInfo;
                        if (propertyInfo != null && propertyInfo.CanWrite)
                        {
                            try
                            {
                                if (excelTitleFieldMapper.IsCheckContentEmpty)
                                {
                                    string cellValue = cell?.ToString();
                                    if (cell != null && cell.CellType == CellType.Formula)
                                    {
                                        cellValue = formulaEvaluator.Evaluate(cell).StringValue;
                                    }
                                    if (string.IsNullOrEmpty(cellValue))
                                    {
                                        t = default(T);
                                        break;
                                    }
                                }

                                if (cell != null && !string.IsNullOrEmpty(cell.ToString()))
                                {
                                    if (excelTitleFieldMapper.IsCoordinateExpress || cell.CellType == CellType.Formula)
                                    {
                                        //读取含有表达式的单元格值
                                        string cellValue = formulaEvaluator.Evaluate(cell).StringValue;
                                        propertyInfo.SetValue(t, Convert.ChangeType(cellValue, propertyInfo.PropertyType));
                                    }
                                    else if (propertyInfo.PropertyType.IsEnum)
                                    {
                                        object enumObj = propertyInfo.PropertyType.InvokeMember(cell.ToString(), BindingFlags.GetField, null, null, null);
                                        propertyInfo.SetValue(t, Convert.ChangeType(enumObj, propertyInfo.PropertyType));
                                    }
                                    else if (propertyInfo.PropertyType == typeof(DateTime))
                                    {
                                        try
                                        {
                                            propertyInfo.SetValue(t, Convert.ChangeType(cell.DateCellValue, propertyInfo.PropertyType));
                                        }
                                        catch (Exception)
                                        {
                                            propertyInfo.SetValue(t, Convert.ChangeType(cell.ToString(), propertyInfo.PropertyType));
                                        }
                                    }
                                    else
                                    {
                                        propertyInfo.SetValue(t, Convert.ChangeType(cell.ToString(), propertyInfo.PropertyType));
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                this.Logger.LogError(e, $"sheetName_{sheet.SheetName},读取{currentRowIndex + 1}行内容失败！");
                            }
                        }
                    }
                    if (t != null)
                    {
                        tList.Add(t);
                    }
                }
            }

            return tList ?? new List<T>(0);
        }


        /// <summary>
        /// 获取文件单元格数据对象
        /// </summary>
        /// <typeparam name="T">T的属性必须标记了ExcelCellAttribute</typeparam>
        /// <param name="filePath">文建路径</param>
        /// <param name="sheetName">sheet名称</param>
        /// <returns></returns>
        public T ReadCellData<T>(string filePath, string sheetName) where T : new()
        {
            T t = new T();

            this.Logger.LogInformation($"开始读取{filePath},sheet名称{sheetName}的数据...");

            CommonCellModelColl commonCellColl = this.ReadCellList(filePath, sheetName);

            Dictionary<PropertyInfo, ExcelCellFieldMapper> propertyMapperDic = ExcelCellFieldMapper.GetModelFieldMapper<T>().ToDictionary(m => m.PropertyInfo);
            string cellExpress = null;
            string pValue = null;
            PropertyInfo propertyInfo = null;
            foreach (var item in propertyMapperDic)
            {
                cellExpress = item.Value.CellCoordinateExpress;
                propertyInfo = item.Key;
                pValue = this.GetVByExpress(cellExpress, propertyInfo, commonCellColl).ToString();
                if (!string.IsNullOrEmpty(pValue))
                {
                    propertyInfo.SetValue(t, Convert.ChangeType(pValue, propertyInfo.PropertyType));
                }
            }
            return t;
        }

        /// <summary>
        /// 读取文件首个Sheet的所有单元格数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public CommonCellModelColl ReadFirstSheetCellList(string filePath)
        {
            IWorkbook workbook = this.CreateWorkbook(filePath);

            ISheet firstSheet = this.GetSheet(workbook);

            CommonCellModelColl commonCellColl = this._ReadCellList(firstSheet);
            return commonCellColl;
        }

        /// <summary>
        /// 读取指定sheet所有单元格数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="sheetName">sheet名称</param>
        /// <returns></returns>
        public CommonCellModelColl ReadCellList(string filePath, string sheetName)
        {
            IWorkbook workbook = this.CreateWorkbook(filePath);

            ISheet sheet = this.GetSheet(workbook, sheetName);

            return this._ReadCellList(sheet);
        }

        private CommonCellModelColl _ReadCellList(ISheet sheet)
        {
            CommonCellModelColl commonCellColl = new CommonCellModelColl(20);

            if (sheet != null)
            {
                var rows = sheet.GetRowEnumerator();
                List<ICell> cellList = null;
                ICell cell = null;

                //从第1行数据开始获取
                while (rows.MoveNext())
                {
                    IRow row = (IRow)rows.Current;

                    cellList = row.Cells;
                    int cellCount = cellList.Count;

                    for (int i = 0; i < cellCount; i++)
                    {
                        cell = cellList[i];
                        CommonCellModel cellModel = new CommonCellModel
                        {
                            RowIndex = row.RowNum,
                            ColumnIndex = i,
                            CellValue = cell.ToString(),
                            IsCellFormula = cell.CellType == CellType.Formula ? true : false
                        };
                        commonCellColl.Add(cellModel);
                    }
                }
            }

            return commonCellColl;
        }

        /// <summary>
        /// 获取文件首个sheet的标题位置
        /// </summary>
        /// <typeparam name="T">T必须做了标题映射</typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public int FileFirstSheetTitleIndex<T>(string filePath)
        {
            int titleIndex = 0;

            if (File.Exists(filePath))
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    IWorkbook workbook = null;
                    try
                    {
                        workbook = new XSSFWorkbook(fileStream);
                    }
                    catch (Exception)
                    {
                        workbook = new HSSFWorkbook(fileStream);
                    }

                    string[] titleArray = ExcelTitleFieldMapper.GetModelFieldMapper<T>().Select(m => m.ExcelTitle).ToArray();

                    ISheet sheet = workbook.GetSheetAt(0);
                    titleIndex = this.GetSheetTitleIndex(sheet, titleArray);
                }
            }

            return titleIndex;
        }

        /// <summary>
        /// 获取文件首个sheet的标题位置
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="titleNames"></param>
        /// <returns></returns>
        public int FileFirstSheetTitleIndex(string filePath, params string[] titleNames)
        {
            int titleIndex = 0;

            if (File.Exists(filePath))
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    IWorkbook workbook = null;
                    try
                    {
                        workbook = new XSSFWorkbook(fileStream);
                    }
                    catch (Exception)
                    {
                        workbook = new HSSFWorkbook(fileStream);
                    }
                    ISheet sheet = workbook.GetSheetAt(0);
                    titleIndex = this.GetSheetTitleIndex(sheet, titleNames);
                }
            }

            return titleIndex;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 返回单元格坐标横坐标
        /// </summary>
        /// <param name="cellPoint">单元格坐标（A1,B15...）</param>
        /// <param name="columnIndex">带回：纵坐标</param>
        /// <returns></returns>
        private int GetValueByZM(string cellPoint, out int columnIndex)
        {
            int rowIndex = 0;
            columnIndex = 0;

            Regex columnIndexRegex = new Regex("[a-zA-Z]+", RegexOptions.IgnoreCase);
            string columnZM = columnIndexRegex.Match(cellPoint).Value;

            rowIndex = Convert.ToInt32(cellPoint.Replace(columnZM, "")) - 1;

            int zmLen = 0;
            if (!string.IsNullOrEmpty(columnZM))
            {
                zmLen = columnZM.Length;
            }
            for (int i = zmLen - 1; i > -1; i--)
            {
                columnIndex += (int)Math.Pow((int)columnZM[i] - 64, (zmLen - i));
            }
            columnIndex = columnIndex - 1;
            return rowIndex;
        }

        /// <summary>
        /// 根据单元格表达式和单元格数据集获取数据
        /// </summary>
        /// <param name="cellExpress">单元格表达式</param>
        /// <param name="commonCellColl">单元格数据集</param>
        /// <returns></returns>
        private object GetVByExpress(string cellExpress, PropertyInfo propertyInfo, CommonCellModelColl commonCellColl)
        {
            object value = null;

            //含有单元格表达式的取表达式值，没有表达式的取单元格字符串
            if (!string.IsNullOrEmpty(cellExpress))
            {
                MatchCollection matchCollection = Regex.Matches(cellExpress, "\\w+");

                string point = null;

                int rowIndex = 0;
                int columnIndex = 0;

                string cellValue = null;
                System.Data.DataTable dt = new System.Data.DataTable();

                foreach (var item in matchCollection)
                {
                    point = item.ToString();
                    rowIndex = this.GetValueByZM(point, out columnIndex);

                    cellValue = commonCellColl[rowIndex, columnIndex]?.CellValue?.ToString() ?? "";

                    if (propertyInfo.PropertyType == typeof(decimal) || propertyInfo.PropertyType == typeof(double) || propertyInfo.PropertyType == typeof(int))
                    {
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            cellValue = cellValue.Replace(",", "");
                        }
                        else
                        {
                            cellValue = "0";
                        }
                    }
                    else
                    {
                        cellValue = $"'{cellValue}'";
                    }
                    cellExpress = cellExpress.Replace(item.ToString(), cellValue);
                }

                //执行字符和数字的表达式计算（字符需要使用单引号包裹，数字需要移除逗号）
                value = dt.Compute(cellExpress, "");
            }

            return value ?? "";

        }

        /// <summary>
        /// 将数据放入单元格中
        /// </summary>
        /// <typeparam name="T">泛型类</typeparam>
        /// <param name="cell">单元格对象</param>
        /// <param name="t">泛型类数据</param>
        /// <param name="cellFieldMapper">单元格映射信息</param>
        private void SetCellValue<T>(ICell cell, T t, ExcelCellFieldMapper cellFieldMapper)
        {
            object cellValue = cellFieldMapper.PropertyInfo.GetValue(t);

            this.SetCellValue(cell, cellValue, cellFieldMapper?.OutputFormat);
        }
        /// <summary>
        /// 将数据放入单元格中
        /// </summary>
        /// <typeparam name="T">泛型类</typeparam>
        /// <param name="cell">单元格对象</param>
        /// <param name="t">泛型类数据</param>
        /// <param name="cellFieldMapper">单元格映射信息</param>
        private void SetCellValue<T>(ICell cell, T t, ExcelTitleFieldMapper cellFieldMapper)
        {
            object cellValue = cellFieldMapper.PropertyInfo.GetValue(t);

            this.SetCellValue(cell, cellValue, cellFieldMapper?.OutputFormat, cellFieldMapper?.IsCoordinateExpress ?? false);
        }

        /// <summary>
        /// 将数据放入单元格中
        /// </summary>
        /// <param name="cell">单元格对象</param>
        /// <param name="cellValue">数据</param>
        /// <param name="outputFormat">格式化字符串</param>
        /// <param name="isCoordinateExpress">是否是表达式数据</param>
        private void SetCellValue(ICell cell, object cellValue, string outputFormat, bool isCoordinateExpress = false)
        {
            if (cell != null && cellValue != null)
            {
                if (isCoordinateExpress)
                {
                    cell.SetCellFormula(cellValue.ToString());
                }
                else
                {
                    if (!string.IsNullOrEmpty(outputFormat))
                    {
                        string formatValue = null;
                        IFormatProvider formatProvider = null;
                        if (cellValue is DateTime)
                        {
                            formatProvider = new DateTimeFormatInfo();
                            ((DateTimeFormatInfo)formatProvider).ShortDatePattern = outputFormat;
                        }
                        formatValue = ((IFormattable)cellValue).ToString(outputFormat, formatProvider);

                        cell.SetCellValue(formatValue);
                    }
                    else
                    {
                        if (cellValue is decimal || cellValue is double || cellValue is int)
                        {
                            cell.SetCellValue(Convert.ToDouble(cellValue));
                        }
                        else if (cellValue is DateTime)
                        {
                            if ((DateTime)cellValue > DateTime.MinValue)
                            {
                                cell.SetCellValue((DateTime)cellValue);
                            }
                        }
                        else if (cellValue is bool)
                        {
                            cell.SetCellValue((bool)cellValue);
                        }
                        else
                        {
                            string cellValueStr = cellValue.ToString();
                            if (cellValueStr.Length > 32767)
                            {
                                cellValueStr = cellValueStr.Substring(0, 32764) + "...";
                            }
                            cell.SetCellValue(cellValueStr);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// 根据标题名称获取标题行下标位置
        /// </summary>
        /// <param name="sheet">要查找的sheet</param>
        /// <param name="titleNames">标题名称</param>
        /// <returns></returns>
        private int GetSheetTitleIndex(ISheet sheet, params string[] titleNames)
        {
            int titleIndex = -1;

            if (sheet != null && titleNames != null && titleNames.Length > 0)
            {
                var rows = sheet.GetRowEnumerator();
                List<ICell> cellList = null;
                List<string> rowValueList = null;

                //从第1行数据开始获取
                while (rows.MoveNext())
                {
                    IRow row = (IRow)rows.Current;

                    cellList = row.Cells;
                    rowValueList = new List<string>(cellList.Count);
                    foreach (var cell in cellList)
                    {
                        rowValueList.Add(cell.ToString());
                    }

                    bool isTitle = true;
                    foreach (var title in titleNames)
                    {
                        if (!rowValueList.Contains(title))
                        {
                            isTitle = false;
                            break;
                        }
                    }
                    if (isTitle)
                    {
                        titleIndex = row.RowNum;
                        break;
                    }
                }
            }
            return titleIndex;
        }

        #endregion

    }

}
