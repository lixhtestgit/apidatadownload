using System;

namespace WebApplication1.DB.Extend
{
    /// <summary>
    /// 数据库属性映射类
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class PropertyMapperAttribute : Attribute
    {
        /// <summary>
        /// 数据库列名
        /// </summary>
        public string DBColumnName { get; set; }

        /// <summary>
        /// 是否是主键列
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// 是否忽略该列
        /// </summary>
        public bool Ignored { get; set; }

        /// <summary>
        /// 构造
        /// </summary>
        public PropertyMapperAttribute() : this(null, false, false)
        {

        }
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="dbColumnName"></param>
        public PropertyMapperAttribute(string dbColumnName) : this(dbColumnName, false, false)
        {

        }
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="isPrimaryKey"></param>
        public PropertyMapperAttribute(bool isPrimaryKey) : this(null, false, isPrimaryKey)
        {
        }
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="dbColumnName"></param>
        /// <param name="isPrimaryKey"></param>
        public PropertyMapperAttribute(string dbColumnName, bool isPrimaryKey) : this(dbColumnName, false, isPrimaryKey)
        {
        }
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="dbColumnName"></param>
        /// <param name="ignored"></param>
        /// <param name="isPrimaryKey"></param>
        public PropertyMapperAttribute(string dbColumnName, bool ignored, bool isPrimaryKey)
        {
            DBColumnName = dbColumnName;
            Ignored = ignored;
            IsPrimaryKey = isPrimaryKey;
        }
    }
}
