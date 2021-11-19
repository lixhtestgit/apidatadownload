using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPPayReportTools.Excel
{
    public class CommonCellModel
    {
        public CommonCellModel() { }

        public CommonCellModel(int rowIndex, int columnIndex, object cellValue, bool isCellFormula = false)
        {
            this.RowIndex = rowIndex;
            this.ColumnIndex = columnIndex;
            this.CellValue = cellValue;
            this.IsCellFormula = isCellFormula;
        }

        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
        public object CellValue { get; set; }

        /// <summary>
        /// 是否是单元格公式
        /// </summary>
        public bool IsCellFormula { get; set; }

    }

    public class CommonCellModelColl : List<CommonCellModel>, IList<CommonCellModel>
    {
        public CommonCellModelColl() { }
        public CommonCellModelColl(int capacity) : base(capacity)
        {

        }

        /// <summary>
        /// 根据行下标，列下标获取单元格数据
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public CommonCellModel this[int rowIndex, int columnIndex]
        {
            get
            {
                CommonCellModel cell = this.FirstOrDefault(m => m.RowIndex == rowIndex && m.ColumnIndex == columnIndex);
                return cell;
            }
            set
            {
                CommonCellModel cell = this.FirstOrDefault(m => m.RowIndex == rowIndex && m.ColumnIndex == columnIndex);
                if (cell != null)
                {
                    cell.CellValue = value.CellValue;
                }
            }
        }

        /// <summary>
        /// 所有一行所有的单元格数据
        /// </summary>
        /// <param name="rowIndex">行下标</param>
        /// <returns></returns>
        public List<CommonCellModel> GetRawCellList(int rowIndex)
        {
            List<CommonCellModel> cellList = null;
            cellList = this.FindAll(m => m.RowIndex == rowIndex);

            return cellList ?? new List<CommonCellModel>(0);
        }

        /// <summary>
        /// 所有一列所有的单元格数据
        /// </summary>
        /// <param name="columnIndex">列下标</param>
        /// <returns></returns>
        public List<CommonCellModel> GetColumnCellList(int columnIndex)
        {
            List<CommonCellModel> cellList = null;
            cellList = this.FindAll(m => m.ColumnIndex == columnIndex);

            return cellList ?? new List<CommonCellModel>(0);
        }

    }
}
