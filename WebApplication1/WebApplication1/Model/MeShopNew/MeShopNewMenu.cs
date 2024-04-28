using PPPayReportTools.Excel;

namespace WebApplication1.Model.MeShopNew
{
    /// <summary>
    /// 菜单
    /// </summary>
    public class MeShopNewMenu
    {
        /// <summary>
        /// 自增ID
        /// </summary>
        [ExcelTitle("ID")]
        public string ID { get; set; }

        /// <summary>
        /// 父ID
        /// </summary>
        [ExcelTitle("ParentID")]
        public string ParentID { get; set; }

        /// <summary>
        /// 类型(0=菜单,1=页面)
        /// </summary>
        [ExcelTitle("Type")]
        public int Type { get; set; }

        /// <summary>
        /// 权限名称
        /// </summary>
        [ExcelTitle("Name")]
        public string Name { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        [ExcelTitle("Icon")]
        public string Icon { get; set; }

        /// <summary>
        /// 导航连接
        /// </summary>
        [ExcelTitle("Href")]
        public string Href { get; set; }

        /// <summary>
        /// 是否可用
        /// </summary>
        public int IsEnable
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// 排序
        /// </summary>
        [ExcelTitle("排序值")]
        public int Sort { get; set; }

    }
}
