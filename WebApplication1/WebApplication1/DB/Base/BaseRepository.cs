using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WebApplication1.DB.Extend;
using WebApplication1.Enum;
using WebApplication1.Helper;
using WebApplication1.Model;

namespace WebApplication1.DB.Base
{
    /// <summary>
    /// 仓储基类
    /// </summary>
    public class BaseRepository
    {
        /// <summary>
        /// 配置执行超时时间120（秒）
        /// </summary>
        private int commandTimeout = 1200000;

        private ConfigHelper _configHelper { get; set; }
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="configHelper"></param>
        public BaseRepository(ConfigHelper configHelper)
        {
            this._configHelper = configHelper;
        }

        /// <summary>
        /// 获取连接串
        /// </summary>
        /// <param name="siteName"></param>
        /// <returns></returns>
        private ConfigDBConnection getDbConnectionStr(EDBSiteName siteName)
        {
            Dictionary<EDBSiteName, ConfigDBConnection> connectionDic = this._configHelper.GetDBConnectionDic();
            return connectionDic[siteName];
        }

        /// <summary>
        /// 获取连接串
        /// </summary>
        /// <param name="siteName"></param>
        /// <returns></returns>
        private IDbConnection getDbConnection(EDBSiteName siteName)
        {
            IDbConnection dbConnection = null;
            Dictionary<EDBSiteName, ConfigDBConnection> connectionDic = this._configHelper.GetDBConnectionDic();
            ConfigDBConnection configDBConnection = connectionDic[siteName];
            if (configDBConnection.DBConnectionType == EDBConnectionType.SqlServer)
            {
                dbConnection = SqlClientFactory.Instance.CreateConnection();
            }
            else if (configDBConnection.DBConnectionType == EDBConnectionType.MySql)
            {
                dbConnection = MySqlConnector.MySqlConnectorFactory.Instance.CreateConnection();
            }
            else if (configDBConnection.DBConnectionType == EDBConnectionType.PostgreSQL)
            {
                dbConnection = Npgsql.NpgsqlFactory.Instance.CreateConnection();
            }
            dbConnection.ConnectionString = configDBConnection.ConnectionStr;
            return dbConnection;
        }

        /// <summary>
        /// 批量数据插入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="siteName"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public async Task<object> InsertAsync<T>(EDBSiteName siteName, T t)
        {
            object result = 0;

            if (t != null)
            {
                IDbConnection dbConnection = this.getDbConnection(siteName);
                string insertSql = DBMapperHelper.GetInsertSql(siteName, new[] { t });
                insertSql += $"SELECT LAST_INSERT_ID();";

                result = await dbConnection.ExecuteScalarAsync(insertSql, commandTimeout: this.commandTimeout);
            }

            return result;
        }

        /// <summary>
        /// 批量数据插入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="siteName"></param>
        /// <param name="tList"></param>
        /// <returns></returns>
        public async Task<int> InsertListAsync<T>(EDBSiteName siteName, List<T> tList)
        {
            int result = 0;

            if (tList != null && tList.Count > 0)
            {
                IDbConnection dbConnection = this.getDbConnection(siteName);
                string insertSql = DBMapperHelper.GetInsertSql(siteName, tList.ToArray());

                result = await dbConnection.ExecuteAsync(insertSql, commandTimeout: this.commandTimeout);
            }

            return result;
        }

        /// <summary>
        /// 高级批量插入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="siteName"></param>
        /// <param name="insertList"></param>
        public async Task BulkCopyAsync<T>(EDBSiteName siteName, List<T> insertList)
        {
            if (insertList != null && insertList.Count > 0)
            {
                //数据量大于100使用SqlBulkCopy，小于100使用批量sql插入
                if (insertList.Count > 100)
                {
                    DataTable table = new DataTable();

                    DBMapperHelper.DBMapperTable mapperTable = DBMapperHelper.GetModelMapper<T>(siteName);
                    ConfigDBConnection configConn = this.getDbConnectionStr(siteName);
                    string conn = configConn.ConnectionStr;

                    // read the table structure from the database
                    string tableName = $"{mapperTable.SchemaName}.{mapperTable.TableName}";
                    DbDataAdapter dbDataAdapter = null;
                    if (configConn.DBConnectionType == EDBConnectionType.SqlServer)
                    {
                        dbDataAdapter = new SqlDataAdapter($"SELECT TOP 0 * FROM {tableName}", conn);
                    }
                    else if (configConn.DBConnectionType == EDBConnectionType.MySql)
                    {
                        dbDataAdapter = new MySqlConnector.MySqlDataAdapter($"SELECT * FROM {tableName} LIMIT 0", conn);
                    }
                    else if (configConn.DBConnectionType == EDBConnectionType.PostgreSQL)
                    {
                        dbDataAdapter = new Npgsql.NpgsqlDataAdapter($"SELECT TOP 0 * FROM {tableName}", conn);
                    }
                    using (dbDataAdapter)
                    {
                        dbDataAdapter.Fill(table);
                    }

                    int count = insertList.Count;
                    for (var i = 0; i < count; i++)
                    {
                        var row = table.NewRow();
                        foreach (DBMapperHelper.DBMapperProperty item in mapperTable.DBPropertyList)
                        {
                            row[item.DBColumnName] = item.PropertyInfo.GetValue(insertList[i]) ?? DBNull.Value;
                        }

                        table.Rows.Add(row);
                    }


                    using (IDbConnection dbConnection = this.getDbConnection(siteName))
                    {
                        if (dbConnection.State == ConnectionState.Closed)
                        {
                            dbConnection.Open();
                        }

                        if (configConn.DBConnectionType == EDBConnectionType.SqlServer)
                        {
                            using (var bulk = new SqlBulkCopy(dbConnection as SqlConnection))
                            {
                                //设置超时时间=1000秒
                                bulk.BulkCopyTimeout = 1000;

                                bulk.DestinationTableName = tableName;
                                await bulk.WriteToServerAsync(table);
                            }
                        }
                        else if (configConn.DBConnectionType == EDBConnectionType.MySql)
                        {
                            var bulk = new MySqlConnector.MySqlBulkCopy(dbConnection as MySqlConnector.MySqlConnection);
                            //设置超时时间=1000秒
                            bulk.BulkCopyTimeout = 1000;
                            bulk.DestinationTableName = tableName;
                            await bulk.WriteToServerAsync(table);
                        }
                        else if (configConn.DBConnectionType == EDBConnectionType.PostgreSQL)
                        {
                            //NpgSql暂未找到合适的大批量插入方法
                            await this.InsertListAsync(siteName, insertList);
                        }
                    };
                }
                else
                {
                    await this.InsertListAsync(siteName, insertList);
                }
            }
        }

        /// <summary>
        /// 查出多条记录的实体泛型集合
        /// </summary>
        /// <typeparam name="T">泛型T</typeparam>
        /// <returns></returns>
        public async Task<List<T>> SelectAsync<T>(EDBSiteName siteName, Sort<T> sort = null)
        {
            IDbConnection dbConnection = this.getDbConnection(siteName);
            string sql = DBMapperHelper.GetSelectSql<T>(siteName, sort);
            List<T> tList = (await dbConnection.QueryAsync<T>(sql, null, commandTimeout: this.commandTimeout)).ToList();

            return tList ?? new List<T>(0);
        }

        /// <summary>
        /// 查出单条数据
        /// </summary>
        /// <typeparam name="T">泛型T</typeparam>
        /// <param name="siteName">数据库类型</param>
        /// <param name="primaryPropertyValue">主键值</param>
        /// <returns></returns>
        public async Task<T> FirstAsync<T>(EDBSiteName siteName, object primaryPropertyValue)
        {
            T t = default(T);

            if (primaryPropertyValue != null)
            {
                IDbConnection dbConnection = this.getDbConnection(siteName);
                Dictionary<string, object> paramDic = null;
                string sql = DBMapperHelper.GetSelectSql<T>(siteName, primaryPropertyValue);

                t = await dbConnection.QueryFirstOrDefaultAsync<T>(sql, paramDic, commandTimeout: commandTimeout);
            }

            return t;
        }

        /// <summary>
        /// 查出单条数据
        /// </summary>
        /// <typeparam name="T">泛型T</typeparam>
        /// <param name="siteName">数据库类型</param>
        /// <param name="expression">查询表达式</param>
        /// <returns></returns>
        public async Task<T> FirstAsync<T>(EDBSiteName siteName, Expression<Func<T, bool>> expression)
        {
            T t = default(T);

            if (expression != null)
            {
                IDbConnection dbConnection = this.getDbConnection(siteName);
                Dictionary<string, object> paramDic = null;
                string sql = DBMapperHelper.GetSelectSql<T>(siteName, expression, null, out paramDic);

                t = await dbConnection.QueryFirstOrDefaultAsync<T>(sql, paramDic, commandTimeout: commandTimeout);
            }

            return t;
        }

        /// <summary>
        /// 查出多条记录的实体泛型集合
        /// </summary>
        /// <typeparam name="T">泛型T</typeparam>
        /// <returns></returns>
        public async Task<List<T>> SelectAsync<T>(EDBSiteName siteName, Expression<Func<T, bool>> expression, Sort<T> sort = null)
        {
            List<T> tList = null;

            if (expression != null)
            {
                IDbConnection dbConnection = this.getDbConnection(siteName);
                Dictionary<string, object> paramDic = null;
                string sql = DBMapperHelper.GetSelectSql<T>(siteName, expression, sort, out paramDic);

                tList = (await dbConnection.QueryAsync<T>(sql, paramDic, commandTimeout: this.commandTimeout)).ToList();
            }

            return tList ?? new List<T>(0);
        }

        /// <summary>
        /// Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="siteName"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public async Task<List<T>> QueryAsync<T>(EDBSiteName siteName, string sql)
        {
            List<T> tList = null;

            if (!string.IsNullOrEmpty(sql))
            {
                IDbConnection dbConnection = this.getDbConnection(siteName);
                tList = (await dbConnection.QueryAsync<T>(sql, commandTimeout: this.commandTimeout)).ToList();
            }

            return tList ?? new List<T>(0);
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<int> ExecuteAsync(EDBSiteName siteName, string sql, object param)
        {
            int executeNum = 0;

            if (!string.IsNullOrWhiteSpace(sql))
            {
                IDbConnection dbConnection = this.getDbConnection(siteName);
                executeNum = await dbConnection.ExecuteAsync(sql, param, commandTimeout: this.commandTimeout);
            }

            return executeNum;
        }

        /// <summary>
        /// ExecuteScalar
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<object> ExecuteScalarAsync(EDBSiteName siteName, string sql, object param)
        {
            object firstData = null;

            if (!string.IsNullOrWhiteSpace(sql))
            {
                IDbConnection dbConnection = this.getDbConnection(siteName);
                firstData = await dbConnection.ExecuteScalarAsync(sql, param, commandTimeout: this.commandTimeout);
            }

            return firstData;
        }

        /// <summary>
        /// 更新对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="siteName"></param>
        /// <param name="tModel"></param>
        /// <returns></returns>
        public async Task<int> UpdateAsync<T>(EDBSiteName siteName, T tModel)
        {
            int result = 0;
            if (tModel != null)
            {
                IDbConnection dbConnection = this.getDbConnection(siteName);
                string updateSQL = DBMapperHelper.GetUpdateSql(siteName, tModel);
                result = await dbConnection.ExecuteAsync(updateSQL, commandTimeout: this.commandTimeout);
            }
            return result;
        }

        /// <summary>
        /// 删除对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="siteName"></param>
        /// <param name="primaryPropertyValue"></param>
        /// <returns></returns>
        public async Task<int> DeleteAsync<T>(EDBSiteName siteName, object primaryPropertyValue)
        {
            int result = 0;
            if (primaryPropertyValue != null)
            {
                IDbConnection dbConnection = this.getDbConnection(siteName);
                string sql = DBMapperHelper.GetDeleteSql(siteName, primaryPropertyValue);
                if (sql.IsNotNullOrEmpty())
                {
                    result = await dbConnection.ExecuteAsync(sql, commandTimeout: this.commandTimeout);
                }
            }
            return result;
        }

        /// <summary>
        /// 删除对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="siteName"></param>
        /// <param name="tModel"></param>
        /// <returns></returns>
        public async Task<int> DeleteAsync<T>(EDBSiteName siteName, T tModel)
        {
            int result = 0;
            if (tModel != null)
            {
                IDbConnection dbConnection = this.getDbConnection(siteName);
                string sql = DBMapperHelper.GetDeleteSql(siteName, tModel);
                if (sql.IsNotNullOrEmpty())
                {
                    result = await dbConnection.ExecuteAsync(sql, commandTimeout: this.commandTimeout);
                }
            }
            return result;
        }

        /// <summary>
        /// 删除对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="siteName"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public async Task<int> DeleteAsync<T>(EDBSiteName siteName, Expression<Func<T, bool>> expression)
        {
            int result = 0;
            if (expression != null)
            {
                IDbConnection dbConnection = this.getDbConnection(siteName);
                string sql = DBMapperHelper.GetDeleteSql(siteName, expression, out Dictionary<string, object> paramObj);
                result = await dbConnection.ExecuteAsync(sql, paramObj, commandTimeout: this.commandTimeout);
            }
            return result;
        }

        /// <summary>
        /// Count
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="siteName"></param>
        /// <param name="where"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<int> CountAsync<T>(EDBSiteName siteName, string where, object param)
        {
            DBMapperHelper.DBMapperTable dBMapper = DBMapperHelper.GetModelMapper<T>(siteName);

            string whereSql = "";
            if (!string.IsNullOrWhiteSpace(where))
            {
                whereSql = $"where {where}";
            }
            string tableSql = (dBMapper.SchemaName.IsNotNullOrEmpty() ? dBMapper.SchemaName + "." : "") + dBMapper.TableName;

            string countSql = $"select count(1) from {tableSql} {whereSql}";
            int count = Convert.ToInt32(await this.ExecuteScalarAsync(siteName, countSql, param));
            return count;
        }

        /// <summary>
        /// GetList
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="siteName"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="where"></param>
        /// <param name="sortFiled"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<(List<T>, int)> GetPageAsync<T>(EDBSiteName siteName, int pageIndex, int pageSize, string where, string sortFiled, object param = null)
        {
            if (pageIndex <= 0 || pageSize <= 0)
            {
                throw new ArgumentException("pageIndex、pageSize参数错误");
            }

            DBMapperHelper.DBMapperTable dBMapper = DBMapperHelper.GetModelMapper<T>(siteName);
            List<string> fieldList = dBMapper.DBColumnList;

            int begin = pageSize * (pageIndex - 1);
            string sql = string.Empty;
            string tableName = dBMapper.TableName;
            string orderSql = string.Empty;
            int recordCount = await this.CountAsync<T>(siteName, where, param);
            if (!string.IsNullOrWhiteSpace(sortFiled))
            {
                orderSql = "ORDER BY " + sortFiled;
            }
            if (!string.IsNullOrWhiteSpace(where))
            {
                where = " WHERE " + where;
            }
            else
            {
                where = " WHERE 1=1";
            }

            //传统查询
            //sql = $"SELECT {string.Join(',', fieldList)} FROM {tableName} {where} {orderSql} LIMIT {begin},{pageSize}";
            //高性能查询
            //优化查询性能、利用单列(优先主键)查询速度，快速过滤不需要的数据

            string primaryKey = dBMapper.DBPrimaryKey;
            if (primaryKey.IsNullOrEmpty())
            {
                primaryKey = dBMapper.DBColumnList.FirstOrDefault();
            }

            ConfigDBConnection configConn = this.getDbConnectionStr(siteName);

            if (configConn.DBConnectionType == EDBConnectionType.MySql)
            {
                sql = $"SELECT {string.Join(',', fieldList)} FROM {tableName} {where} {orderSql} LIMIT {begin},{pageSize}";
            }
            else if (configConn.DBConnectionType == EDBConnectionType.SqlServer)
            {
                sql = $@"SELECT TOP({pageSize}) {string.Join(',', fieldList)} FROM {tableName} {where} AND {primaryKey} NOT IN (SELECT TOP({begin}) {primaryKey} FROM {tableName} {where} {orderSql}) {orderSql} ";
            }

            IDbConnection dbConnection = this.getDbConnection(siteName);
            List<T> list = (await dbConnection.QueryAsync<T>(sql, param)).ToList();
            return (list, recordCount);
        }

    }

    /// <summary>
    /// 仓储基类
    /// </summary>
    public class BaseRepository<T, TKey> : BaseRepository
    {
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="configHelper"></param>
        public BaseRepository(ConfigHelper configHelper) : base(configHelper)
        {
        }

        /// <summary>
        /// 查出多条记录的实体泛型集合
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public Task<List<T>> Select(EDBSiteName siteName, Sort<T> sort = null)
        {
            return base.SelectAsync(siteName, sort);
        }

        /// <summary>
        /// 查出单条数据
        /// </summary>
        /// <param name="siteName">数据库类型</param>
        /// <param name="primaryPropertyValue">主键值</param>
        /// <returns></returns>
        public async Task<T> First(EDBSiteName siteName, TKey primaryPropertyValue)
        {
            return await base.FirstAsync<T>(siteName, primaryPropertyValue);
        }
        /// <summary>
        /// 查出单条数据
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Task<T> First(EDBSiteName siteName, Expression<Func<T, bool>> expression)
        {
            return base.FirstAsync(siteName, expression);
        }

        /// <summary>
        /// 查出多条记录的实体泛型集合
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="expression"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public Task<List<T>> Select(EDBSiteName siteName, Expression<Func<T, bool>> expression, Sort<T> sort = null)
        {
            return base.SelectAsync(siteName, expression, sort);
        }


        /// <summary>
        /// 插入实体记录
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<object> Insert(EDBSiteName siteName, T entity)
        {
            return await base.InsertAsync(siteName, entity);
        }

        /// <summary>
        /// 批量数据插入
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="tList"></param>
        /// <returns></returns>
        public Task<int> InsertList(EDBSiteName siteName, List<T> tList)
        {
            return base.InsertListAsync(siteName, tList);
        }

        /// <summary>
        /// 更新实体记录
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<int> Update(EDBSiteName siteName, T entity)
        {
            return await base.UpdateAsync(siteName, entity);
        }

        /// <summary>
        /// 删除指定键的记录
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public async Task<int> Delete(EDBSiteName siteName, TKey ID)
        {
            return await base.DeleteAsync(siteName, ID);
        }

        /// <summary>
        /// 删除实体记录
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<int> Delete(EDBSiteName siteName, T entity)
        {
            return await base.DeleteAsync(siteName, entity);
        }

        /// <summary>
        /// 删除所有符合特定表达式的数据
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="whereExpress"></param>
        /// <returns></returns>
        public async Task<int> Delete(EDBSiteName siteName, Expression<Func<T, bool>> whereExpress)
        {
            return await base.DeleteAsync(siteName, whereExpress);
        }
    }
}
