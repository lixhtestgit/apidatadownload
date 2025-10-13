using System;
using WebApplication1.DB.Extend;
using WebApplication1.Enum;

namespace WebApplication1.DB.MeShop
{
    /// <summary>
    /// 订单地址
    /// </summary>
    [ClassMapper(EDBSiteName.Wigsbuyshop, "", "Order_address")]
    public class Order_address
	{
        /// <summary>
        /// 主键
        /// </summary>
        [PropertyMapper(isPrimaryKey: true, isIdentityInsert: true)]
        public long ID { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [PropertyMapper]
        public long UserID { get; set; }

        /// <summary>
        /// 订单ID
        /// </summary>
        [PropertyMapper]
        public long OrderID { get; set; }

        /// <summary>
        /// 电子邮箱
        /// </summary>
        [PropertyMapper]
        public string Email { get; set; }

        /// <summary>
        /// 名
        /// </summary>
        [PropertyMapper]
        public string FirstName { get; set; }

        /// <summary>
        /// 姓
        /// </summary>
        [PropertyMapper]
        public string LastName { get; set; }

        /// <summary>
        /// 国家代码
        /// </summary>
        [PropertyMapper]
        public string CountryCode { get; set; }

        /// <summary>
        /// 省份代码
        /// </summary>
        [PropertyMapper]
        public string ProvinceCode { get; set; }

        /// <summary>
        /// 省份
        /// </summary>
        [PropertyMapper]
        public string Province { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        [PropertyMapper]
        public string City { get; set; }

        /// <summary>
        /// 地址行1
        /// </summary>
        [PropertyMapper]
        public string Address1 { get; set; }

        /// <summary>
        /// 地址行2
        /// </summary>
        [PropertyMapper]
        public string Address2 { get; set; }

        /// <summary>
        /// 邮政编码
        /// </summary>
        [PropertyMapper]
        public string ZIP { get; set; }

        /// <summary>
        /// 电话号码
        /// </summary>
        [PropertyMapper]
        public string Phone { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        [PropertyMapper]
        public int Type { get; set; }

        /// <summary>
        /// 是否默认
        /// </summary>
        [PropertyMapper]
        public int IsDefault { get; set; }

        /// <summary>
        /// 区域代码
        /// </summary>
        [PropertyMapper]
        public string AreaCode { get; set; }

        /// <summary>
        /// 国旗图片来源
        /// </summary>
        [PropertyMapper]
        public string NationalFlagSrc { get; set; }
    }
}
