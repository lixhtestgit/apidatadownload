using WebApplication1.Enum;

namespace WebApplication1.DB.Extend
{
    /// <summary>
    /// 数据库表名映射类
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class ClassMapperAttribute : System.Attribute
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        public EDBConnectionType DBConnectionType { get; set; }
        /// <summary>
        /// 数据库表的架构
        /// </summary>
        public string SchemaName { get; set; }
        /// <summary>
        /// 数据库表名称
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="eDBConnectionType"></param>
        /// <param name="schemaName"></param>
        /// <param name="tableName"></param>
        public ClassMapperAttribute(EDBConnectionType eDBConnectionType, string schemaName = null, string tableName = null)
        {
            DBConnectionType = eDBConnectionType;
            SchemaName = schemaName;
            TableName = tableName;
        }
    }
}
