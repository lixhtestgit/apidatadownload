using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using WebApplication1.Enum;

namespace WebApplication1.DB.Extend
{
	/// <summary>
	/// 数据库映射帮助类
	/// </summary>
	public class DBMapperHelper
	{
		/// <summary>
		/// 获取映射
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="siteName"></param>
		/// <returns></returns>
		public static DBMapperTable GetModelMapper<T>(EDBSiteName siteName)
		{
			DBMapperTable mapperTable = new DBMapperTable
			{
				DBPropertyList = new List<DBMapperProperty>(0)
			};

			Type tType = typeof(T);

			IEnumerable<ClassMapperAttribute> classMapperAttributeList = (IEnumerable<ClassMapperAttribute>)tType.GetCustomAttributes(typeof(ClassMapperAttribute));
			ClassMapperAttribute classMapperAttribute = classMapperAttributeList.ToList().FirstOrDefault(m => m.SiteName == siteName);

			if (classMapperAttribute != null)
			{
				mapperTable.SchemaName = classMapperAttribute.SchemaName;
				mapperTable.TableName = classMapperAttribute.TableName;
				if (string.IsNullOrEmpty(mapperTable.TableName))
				{
					mapperTable.TableName = tType.Name;
				}
			}

			List<PropertyInfo> tPropertyInfoList = tType.GetProperties().ToList();
			PropertyMapperAttribute propertyMapAttribute = null;
			foreach (var tPropertyInfo in tPropertyInfoList)
			{
				propertyMapAttribute = (PropertyMapperAttribute)tPropertyInfo.GetCustomAttribute(typeof(PropertyMapperAttribute));

				//属性映射特性&&不忽略
				if (propertyMapAttribute != null && propertyMapAttribute.Ignored == false)
				{
					if (string.IsNullOrEmpty(propertyMapAttribute.DBColumnName))
					{
						propertyMapAttribute.DBColumnName = tPropertyInfo.Name;
					}
					mapperTable.DBPropertyList.Add(new DBMapperProperty
					{
						DBColumnName = propertyMapAttribute.DBColumnName,
						IsPrimaryKey = propertyMapAttribute.IsPrimaryKey,
						PropertyInfo = tPropertyInfo
					});
				}
			}
			return mapperTable;
		}

		/// <summary>
		/// 获取插入语句
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="siteName"></param>
		/// <param name="tArray"></param>
		/// <returns></returns>
		public static string GetInsertSql<T>(EDBSiteName siteName, params T[] tArray)
		{
			DBMapperTable dBMapper = GetModelMapper<T>(siteName);

			List<string> columnSqlList = dBMapper.DBColumnList;

			List<string> valuesSqlList = new List<string>(tArray.Length);
			foreach (var tModel in tArray)
			{
				List<string> tValueList = new List<string>(dBMapper.DBPropertyList.Count);
				foreach (DBMapperProperty dbMapperProperty in dBMapper.DBPropertyList)
				{
					string paramValue = FormatDBValue(dbMapperProperty.PropertyInfo.GetValue(tModel));

					tValueList.Add(paramValue);
				}
				valuesSqlList.Add($"({string.Join(',', tValueList)})");
			}

			string tableSql = (dBMapper.SchemaName.IsNotNullOrEmpty() ? dBMapper.SchemaName + "." : "") + dBMapper.TableName;
			string insertSql = $"insert into {tableSql}({string.Join(',', columnSqlList)}) values {string.Join(',', valuesSqlList)};";
			return insertSql;
		}

		/// <summary>
		/// 获取更新语句
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="siteName"></param>
		/// <param name="tModel"></param>
		/// <returns></returns>
		public static string GetUpdateSql<T>(EDBSiteName siteName, T tModel)
		{
			DBMapperTable dBMapper = GetModelMapper<T>(siteName);

			List<string> updatePropertySQLList = new List<string>(dBMapper.DBPropertyList.Count);
			foreach (DBMapperProperty dbMapperProperty in dBMapper.DBPropertyList)
			{
				if (dbMapperProperty.IsPrimaryKey == false)
				{
					updatePropertySQLList.Add($"{dbMapperProperty.DBColumnName}={FormatDBValue(dbMapperProperty.PropertyInfo.GetValue(tModel))}");
				}
			}

			DBMapperProperty primaryProperty = dBMapper.DBPrimaryProperty;
			string tableSql = (dBMapper.SchemaName.IsNotNullOrEmpty() ? dBMapper.SchemaName + "." : "") + dBMapper.TableName;
			string updateSQL = $"update {tableSql} set {string.Join(',', updatePropertySQLList)} where {primaryProperty.DBColumnName}={FormatDBValue(primaryProperty.PropertyInfo.GetValue(tModel))}";

			return updateSQL;
		}

		/// <summary>
		/// 获取删除语句
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="siteName"></param>
		/// <param name="primaryPropertyValue"></param>
		/// <returns></returns>
		public static string GetDeleteSql<T>(EDBSiteName siteName, object primaryPropertyValue)
		{
			string updateSQL = null;

			DBMapperTable dBMapper = GetModelMapper<T>(siteName);
			DBMapperProperty primaryProperty = dBMapper.DBPrimaryProperty;
			if (primaryProperty != null)
			{
				string tableSql = (dBMapper.SchemaName.IsNotNullOrEmpty() ? dBMapper.SchemaName + "." : "") + dBMapper.TableName;
				updateSQL = $"delete from {tableSql} where {primaryProperty.DBColumnName}={FormatDBValue(primaryPropertyValue)}";
			}
			return updateSQL ?? "";
		}

		/// <summary>
		/// 获取删除语句
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="siteName"></param>
		/// <param name="tModel"></param>
		/// <returns></returns>
		public static string GetDeleteSql<T>(EDBSiteName siteName, T tModel)
		{
			DBMapperTable dBMapper = GetModelMapper<T>(siteName);

			DBMapperProperty primaryProperty = dBMapper.DBPrimaryProperty;
			string tableSql = (dBMapper.SchemaName.IsNotNullOrEmpty() ? dBMapper.SchemaName + "." : "") + dBMapper.TableName;
			string updateSQL = $"delete from {tableSql} where {primaryProperty.DBColumnName}={FormatDBValue(primaryProperty.PropertyInfo.GetValue(tModel))}";

			return updateSQL;
		}

		/// <summary>
		/// 获取删除语句
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="siteName"></param>
		/// <param name="whereExpression"></param>
		/// <param name="paramObj"></param>
		/// <returns></returns>
		public static string GetDeleteSql<T>(EDBSiteName siteName, Expression<Func<T, bool>> whereExpression, out Dictionary<string, object> paramObj)
		{
			DBMapperTable dBMapper = GetModelMapper<T>(siteName);

			string tableSql = (dBMapper.SchemaName.IsNotNullOrEmpty() ? dBMapper.SchemaName + "." : "") + dBMapper.TableName;
			string sql = $"delete from {tableSql}";

			if (whereExpression != null)
			{
				WherePart wherePart = new WhereBuilder().ToSql(whereExpression);

				sql += $"where {wherePart.Sql}";
				ExpandoObject param = new ExpandoObject();
				foreach (var item in wherePart.Parameters)
				{
					((IDictionary<string, object>)param).Add(item.Key, item.Value);
				}
				paramObj = wherePart.Parameters;
			}
			else
			{
				paramObj = null;
			}

			return sql;
		}

		/// <summary>
		/// 获取查询语句
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="siteName"></param>
		/// <param name="primaryPropertyValue"></param>
		/// <returns></returns>
		public static string GetSelectSql<T>(EDBSiteName siteName, object primaryPropertyValue)
		{
			string sql = null;

			DBMapperTable dBMapper = GetModelMapper<T>(siteName);
			DBMapperProperty primaryProperty = dBMapper.DBPrimaryProperty;
			if (primaryProperty != null)
			{
				string tableSql = (dBMapper.SchemaName.IsNotNullOrEmpty() ? dBMapper.SchemaName + "." : "") + dBMapper.TableName;
				sql = $"select * from {tableSql} where {primaryProperty.DBColumnName}={FormatDBValue(primaryPropertyValue)}";
			}
			return sql ?? "";
		}

		/// <summary>
		/// 获取查询语句
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="siteName"></param>
		/// <param name="sort"></param>
		/// <returns></returns>
		public static string GetSelectSql<T>(EDBSiteName siteName, Sort<T> sort)
		{
			string sql = GetSelectSql(siteName, null, sort, out Dictionary<string, object> paramObj);

			return sql;
		}

		/// <summary>
		/// 获取查询语句
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="siteName"></param>
		/// <param name="whereExpression"></param>
		/// <param name="sort"></param>
		/// <param name="paramObj"></param>
		/// <returns></returns>
		public static string GetSelectSql<T>(EDBSiteName siteName, Expression<Func<T, bool>> whereExpression, Sort<T> sort, out Dictionary<string, object> paramObj)
		{
			DBMapperTable dBMapper = GetModelMapper<T>(siteName);

			List<string> columnSqlList = dBMapper.DBColumnList;
			string tableSql = (dBMapper.SchemaName.IsNotNullOrEmpty() ? dBMapper.SchemaName + "." : "") + dBMapper.TableName;
			string sql = $"select {string.Join(',', columnSqlList)} from {tableSql}";

			if (whereExpression != null)
			{
				WherePart wherePart = new WhereBuilder().ToSql(whereExpression);

				sql += $" where {wherePart.Sql}";
				ExpandoObject param = new ExpandoObject();
				foreach (var item in wherePart.Parameters)
				{
					((IDictionary<string, object>)param).Add(item.Key, item.Value);
				}
				paramObj = wherePart.Parameters;
			}
			else
			{
				paramObj = null;
			}

			if (sort != null)
			{
				string sortSql = sort.ToSql();
				if (!string.IsNullOrWhiteSpace(sortSql))
				{
					sql += $" order by {sortSql}";
				}
			}

			return sql;
		}

		/// <summary>
		/// 格式化数据库值
		/// </summary>
		/// <param name="paramValue"></param>
		/// <returns></returns>
		private static string FormatDBValue(object paramValue)
		{
			string param = paramValue?.ToString();

			if (string.IsNullOrEmpty(param))
			{
				param = "NULL";
			}
			else
			{
				if (param.Contains("'"))
				{
					param = paramValue.ToString().Replace("'", "''");
				}
				if (paramValue is string || paramValue is Guid)
				{
					param = $"'{param}'";
				}
				else if (paramValue is DateTime || paramValue is DateTime?)
				{
					param = "'" + Convert.ToDateTime(param).ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
				}
			}

			return param;
		}

		/// <summary>
		/// 数据库映射表
		/// </summary>
		public class DBMapperTable
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
			/// 数据库列列表
			/// </summary>
			public List<DBMapperProperty> DBPropertyList { get; set; }

			/// <summary>
			/// 数据库主键Key
			/// </summary>
			public string DBPrimaryKey
			{
				get
				{
					return DBPropertyList.FirstOrDefault(m => m.IsPrimaryKey)?.DBColumnName ?? "";
				}
			}

			/// <summary>
			/// 数据库主键属性
			/// </summary>
			public DBMapperProperty DBPrimaryProperty
			{
				get
				{
					return DBPropertyList.FirstOrDefault(m => m.IsPrimaryKey);
				}
			}

			/// <summary>
			/// 数据库列名列表
			/// </summary>
			public List<string> DBColumnList
			{
				get
				{
					return DBPropertyList.Select(m => m.DBColumnName).ToList();
				}
			}
		}

		/// <summary>
		/// 数据库映射属性
		/// </summary>
		public class DBMapperProperty
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
			/// 属性信息
			/// </summary>
			public PropertyInfo PropertyInfo { get; set; }
		}

	}
}
