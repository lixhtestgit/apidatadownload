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
        private int commandTimeout = 120;

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
        /// <param name="eDBConnectionType"></param>
        /// <returns></returns>
        private string getDbConnectionStr(EDBConnectionType eDBConnectionType)
        {
            Dictionary<EDBConnectionType, ConfigDBConnection> connectionDic = this._configHelper.GetDBConnectionDic();
            ConfigDBConnection configDBConnection = connectionDic[eDBConnectionType];
            return configDBConnection.ConnectionStr;
        }

        /// <summary>
        /// 获取连接串
        /// </summary>
        /// <param name="eDBConnectionType"></param>
        /// <returns></returns>
        private IDbConnection getDbConnection(EDBConnectionType eDBConnectionType)
        {
            IDbConnection dbConnection = null;
            Dictionary<EDBConnectionType, ConfigDBConnection> connectionDic = this._configHelper.GetDBConnectionDic();
            ConfigDBConnection configDBConnection = connectionDic[eDBConnectionType];
            if (eDBConnectionType == EDBConnectionType.SqlServer)
            {
                dbConnection = SqlClientFactory.Instance.CreateConnection();
            }
            else if (eDBConnectionType == EDBConnectionType.MySql)
            {
                dbConnection = MySqlConnector.MySqlConnectorFactory.Instance.CreateConnection();
            }
            else if (eDBConnectionType == EDBConnectionType.PostgreSQL)
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
        /// <param name="eDBConnectionType"></param>
        /// <param name="tList"></param>
        /// <returns></returns>
        public async Task<int> InsertListAsync<T>(EDBConnectionType eDBConnectionType, List<T> tList)
        {
            int result = 0;

            if (tList != null && tList.Count > 0)
            {
                IDbConnection dbConnection = this.getDbConnection(eDBConnectionType);
                string insertSql = DBMapperHelper.GetInsertSql(eDBConnectionType, tList.ToArray());
                result = await dbConnection.ExecuteAsync(insertSql, commandTimeout: this.commandTimeout);
            }

            return result;
        }

        /// <summary>
        /// 高级批量插入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eDBConnectionType"></param>
        /// <param name="insertList"></param>
        public async Task BulkCopyAsync<T>(EDBConnectionType eDBConnectionType, List<T> insertList)
        {
            if (insertList != null && insertList.Count > 0)
            {
                //数据量大于100使用SqlBulkCopy，小于100使用批量sql插入
                if (insertList.Count > 100)
                {
                    DataTable table = new DataTable();

                    DBMapperHelper.DBMapperTable mapperTable = DBMapperHelper.GetModelMapper<T>(eDBConnectionType);
                    string conn = this.getDbConnectionStr(eDBConnectionType);

                    // read the table structure from the database
                    string tableName = $"{mapperTable.SchemaName}.{mapperTable.TableName}";
                    DbDataAdapter dbDataAdapter = null;
                    if (eDBConnectionType == EDBConnectionType.SqlServer)
                    {
                        dbDataAdapter = new SqlDataAdapter($"SELECT TOP 0 * FROM {tableName}", conn);
                    }
                    else if (eDBConnectionType == EDBConnectionType.MySql)
                    {
                        dbDataAdapter = new MySqlConnector.MySqlDataAdapter($"SELECT * FROM {tableName} LIMIT 0", conn);
                    }
                    else if (eDBConnectionType == EDBConnectionType.PostgreSQL)
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


                    using (IDbConnection dbConnection = this.getDbConnection(eDBConnectionType))
                    {
                        if (dbConnection.State == ConnectionState.Closed)
                        {
                            dbConnection.Open();
                        }

                        if (eDBConnectionType == EDBConnectionType.SqlServer)
                        {
                            using (var bulk = new SqlBulkCopy(dbConnection as SqlConnection))
                            {
                                //设置超时时间=1000秒
                                bulk.BulkCopyTimeout = 1000;

                                bulk.DestinationTableName = tableName;
                                await bulk.WriteToServerAsync(table);
                            }
                        }
                        else if (eDBConnectionType == EDBConnectionType.MySql)
                        {
                            var bulk = new MySqlConnector.MySqlBulkCopy(dbConnection as MySqlConnector.MySqlConnection);
                            //设置超时时间=1000秒
                            bulk.BulkCopyTimeout = 1000;
                            bulk.DestinationTableName = tableName;
                            await bulk.WriteToServerAsync(table);
                        }
                        else if (eDBConnectionType == EDBConnectionType.PostgreSQL)
                        {
                            //NpgSql暂未找到合适的大批量插入方法
                            await this.InsertListAsync(eDBConnectionType, insertList);
                        }
                    };
                }
                else
                {
                    await this.InsertListAsync(eDBConnectionType, insertList);
                }
            }
        }

        /// <summary>
        /// 查出多条记录的实体泛型集合
        /// </summary>
        /// <typeparam name="T">泛型T</typeparam>
        /// <returns></returns>
        public async Task<List<T>> SelectAsync<T>(EDBConnectionType eDBConnectionType, Sort<T> sort = null)
        {
            IDbConnection dbConnection = this.getDbConnection(eDBConnectionType);
            string sql = DBMapperHelper.GetSelectSql<T>(eDBConnectionType, sort);
            List<T> tList = (await dbConnection.QueryAsync<T>(sql, null, commandTimeout: this.commandTimeout)).ToList();

            return tList ?? new List<T>(0);
        }

        /// <summary>
        /// 查出单条数据
        /// </summary>
        /// <typeparam name="T">泛型T</typeparam>
        /// <param name="eDBConnectionType">数据库类型</param>
        /// <param name="primaryPropertyValue">主键值</param>
        /// <returns></returns>
        public async Task<T> FirstAsync<T>(EDBConnectionType eDBConnectionType, object primaryPropertyValue)
        {
            T t = default(T);

            if (primaryPropertyValue != null)
            {
                IDbConnection dbConnection = this.getDbConnection(eDBConnectionType);
                Dictionary<string, object> paramDic = null;
                string sql = DBMapperHelper.GetSelectSql<T>(eDBConnectionType, primaryPropertyValue);

                t = await dbConnection.QueryFirstOrDefaultAsync<T>(sql, paramDic, commandTimeout: commandTimeout);
            }

            return t;
        }

        /// <summary>
        /// 查出单条数据
        /// </summary>
        /// <typeparam name="T">泛型T</typeparam>
        /// <param name="eDBConnectionType">数据库类型</param>
        /// <param name="expression">查询表达式</param>
        /// <returns></returns>
        public async Task<T> FirstAsync<T>(EDBConnectionType eDBConnectionType, Expression<Func<T, bool>> expression)
        {
            T t = default(T);

            if (expression != null)
            {
                IDbConnection dbConnection = this.getDbConnection(eDBConnectionType);
                Dictionary<string, object> paramDic = null;
                string sql = DBMapperHelper.GetSelectSql<T>(eDBConnectionType, expression, null, out paramDic);

                t = await dbConnection.QueryFirstOrDefaultAsync<T>(sql, paramDic, commandTimeout: commandTimeout);
            }

            return t;
        }

        /// <summary>
        /// 查出多条记录的实体泛型集合
        /// </summary>
        /// <typeparam name="T">泛型T</typeparam>
        /// <returns></returns>
        public async Task<List<T>> SelectAsync<T>(EDBConnectionType eDBConnectionType, Expression<Func<T, bool>> expression, Sort<T> sort = null)
        {
            List<T> tList = null;

            if (expression != null)
            {
                IDbConnection dbConnection = this.getDbConnection(eDBConnectionType);
                Dictionary<string, object> paramDic = null;
                string sql = DBMapperHelper.GetSelectSql<T>(eDBConnectionType, expression, sort, out paramDic);

                tList = (await dbConnection.QueryAsync<T>(sql, paramDic, commandTimeout: this.commandTimeout)).ToList();
            }

            return tList ?? new List<T>(0);
        }

        /// <summary>
        /// Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eDBConnectionType"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public async Task<List<T>> QueryAsync<T>(EDBConnectionType eDBConnectionType, string sql)
        {
            List<T> tList = null;

            if (!string.IsNullOrEmpty(sql))
            {
                IDbConnection dbConnection = this.getDbConnection(eDBConnectionType);
                tList = (await dbConnection.QueryAsync<T>(sql, commandTimeout: this.commandTimeout)).ToList();
            }

            return tList ?? new List<T>(0);
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="eDBConnectionType"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<int> ExecuteAsync(EDBConnectionType eDBConnectionType, string sql, object param)
        {
            int executeNum = 0;

            if (!string.IsNullOrWhiteSpace(sql))
            {
                IDbConnection dbConnection = this.getDbConnection(eDBConnectionType);
                executeNum = await dbConnection.ExecuteAsync(sql, param, commandTimeout: this.commandTimeout);
            }

            return executeNum;
        }

        /// <summary>
        /// ExecuteScalar
        /// </summary>
        /// <param name="eDBConnectionType"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<object> ExecuteScalar(EDBConnectionType eDBConnectionType, string sql, object param)
        {
            object firstData = null;

            if (!string.IsNullOrWhiteSpace(sql))
            {
                IDbConnection dbConnection = this.getDbConnection(eDBConnectionType);
                firstData = await dbConnection.ExecuteScalarAsync(sql, param, commandTimeout: this.commandTimeout);
            }

            return firstData;
        }

        /// <summary>
        /// 更新对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eDBConnectionType"></param>
        /// <param name="tModel"></param>
        /// <returns></returns>
        public async Task<int> Update<T>(EDBConnectionType eDBConnectionType, T tModel)
        {
            int result = 0;
            if (tModel != null)
            {
                IDbConnection dbConnection = this.getDbConnection(eDBConnectionType);
                string updateSQL = DBMapperHelper.GetUpdateSql(eDBConnectionType, tModel);
                result = await dbConnection.ExecuteAsync(updateSQL, commandTimeout: this.commandTimeout);
            }
            return result;
        }

        /// <summary>
        /// 删除对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eDBConnectionType"></param>
        /// <param name="primaryPropertyValue"></param>
        /// <returns></returns>
        public async Task<int> Delete<T>(EDBConnectionType eDBConnectionType, object primaryPropertyValue)
        {
            int result = 0;
            if (primaryPropertyValue != null)
            {
                IDbConnection dbConnection = this.getDbConnection(eDBConnectionType);
                string sql = DBMapperHelper.GetDeleteSql(eDBConnectionType, primaryPropertyValue);
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
        /// <param name="eDBConnectionType"></param>
        /// <param name="tModel"></param>
        /// <returns></returns>
        public async Task<int> Delete<T>(EDBConnectionType eDBConnectionType, T tModel)
        {
            int result = 0;
            if (tModel != null)
            {
                IDbConnection dbConnection = this.getDbConnection(eDBConnectionType);
                string sql = DBMapperHelper.GetDeleteSql(eDBConnectionType, tModel);
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
        /// <param name="eDBConnectionType"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public async Task<int> Delete<T>(EDBConnectionType eDBConnectionType, Expression<Func<T, bool>> expression)
        {
            int result = 0;
            if (expression != null)
            {
                IDbConnection dbConnection = this.getDbConnection(eDBConnectionType);
                string sql = DBMapperHelper.GetDeleteSql(eDBConnectionType, expression, out Dictionary<string, object> paramObj);
                result = await dbConnection.ExecuteAsync(sql, paramObj, commandTimeout: this.commandTimeout);
            }
            return result;
        }

        /// <summary>
        /// Count
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eDBConnectionType"></param>
        /// <param name="where"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<int> Count<T>(EDBConnectionType eDBConnectionType, string where, object param)
        {
            DBMapperHelper.DBMapperTable dBMapper = DBMapperHelper.GetModelMapper<T>(eDBConnectionType);

            string whereSql = "";
            if (!string.IsNullOrWhiteSpace(where))
            {
                whereSql = $"where {where}";
            }

            string countSql = $"select count(1) from {dBMapper.SchemaName}.{dBMapper.TableName} {whereSql}";
            int count = Convert.ToInt32(await this.ExecuteScalar(eDBConnectionType, countSql, param));
            return count;
        }

        /// <summary>
        /// GetList
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eDBConnectionType"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="where"></param>
        /// <param name="sortFiled"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<(List<T>, int)> GetList<T>(EDBConnectionType eDBConnectionType, int pageIndex, int pageSize, string where, string sortFiled, object param = null)
        {
            if (pageIndex <= 0 || pageSize <= 0)
            {
                throw new ArgumentException("pageIndex、pageSize参数错误");
            }

            DBMapperHelper.DBMapperTable dBMapper = DBMapperHelper.GetModelMapper<T>(eDBConnectionType);
            List<string> fieldList = dBMapper.DBColumnList;

            int begin = pageSize * (pageIndex - 1);
            string sql = string.Empty;
            string tableName = dBMapper.TableName;
            string orderSql = string.Empty;
            int recordCount = await this.Count<T>(eDBConnectionType, where, param);
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

            if (eDBConnectionType == EDBConnectionType.MySql)
            {
                sql = $"SELECT {string.Join(',', fieldList)} FROM {tableName} {where} {orderSql} LIMIT {begin},{pageSize}";
            }
            else if (eDBConnectionType == EDBConnectionType.SqlServer)
            {
                sql = $@"SELECT TOP({pageSize}) {string.Join(',', fieldList)} FROM {tableName} {where} AND {primaryKey} NOT IN (SELECT TOP({begin}) {primaryKey} FROM {tableName} {where} {orderSql}) {orderSql} ";
            }

            IDbConnection dbConnection = this.getDbConnection(eDBConnectionType);
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
        /// <param name="eDBConnectionType"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public Task<List<T>> Select(EDBConnectionType eDBConnectionType, Sort<T> sort = null)
        {
            return base.SelectAsync(eDBConnectionType, sort);
        }

        /// <summary>
        /// 查出单条数据
        /// </summary>
        /// <param name="eDBConnectionType">数据库类型</param>
        /// <param name="primaryPropertyValue">主键值</param>
        /// <returns></returns>
        public async Task<T> First(EDBConnectionType eDBConnectionType, TKey primaryPropertyValue)
        {
            return await base.FirstAsync<T>(eDBConnectionType, primaryPropertyValue);
        }
        /// <summary>
        /// 查出单条数据
        /// </summary>
        /// <param name="eDBConnectionType"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Task<T> First(EDBConnectionType eDBConnectionType, Expression<Func<T, bool>> expression)
        {
            return base.FirstAsync(eDBConnectionType, expression);
        }

        /// <summary>
        /// 查出多条记录的实体泛型集合
        /// </summary>
        /// <param name="eDBConnectionType"></param>
        /// <param name="expression"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public Task<List<T>> Select(EDBConnectionType eDBConnectionType, Expression<Func<T, bool>> expression, Sort<T> sort = null)
        {
            return base.SelectAsync(eDBConnectionType, expression, sort);
        }


        /// <summary>
        /// 插入实体记录
        /// </summary>
        /// <param name="eDBConnectionType"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<int> Insert(EDBConnectionType eDBConnectionType, T entity)
        {
            return await base.InsertListAsync(eDBConnectionType, new List<T> { entity });
        }

        /// <summary>
        /// 批量数据插入
        /// </summary>
        /// <param name="eDBConnectionType"></param>
        /// <param name="tList"></param>
        /// <returns></returns>
        public Task<int> InsertList(EDBConnectionType eDBConnectionType, List<T> tList)
        {
            return base.InsertListAsync(eDBConnectionType, tList);
        }

        /// <summary>
        /// 更新实体记录
        /// </summary>
        /// <param name="eDBConnectionType"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<int> Update(EDBConnectionType eDBConnectionType, T entity)
        {
            return await base.Update(eDBConnectionType, entity);
        }

        /// <summary>
        /// 删除指定键的记录
        /// </summary>
        /// <param name="eDBConnectionType"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public async Task<int> Delete(EDBConnectionType eDBConnectionType, TKey ID)
        {
            return await base.Delete(eDBConnectionType, ID);
        }

        /// <summary>
        /// 删除实体记录
        /// </summary>
        /// <param name="eDBConnectionType"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<int> Delete(EDBConnectionType eDBConnectionType, T entity)
        {
            return await base.Delete(eDBConnectionType, entity);
        }

        /// <summary>
        /// 删除所有符合特定表达式的数据
        /// </summary>
        /// <param name="eDBConnectionType"></param>
        /// <param name="whereExpress"></param>
        /// <returns></returns>
        public async Task<int> Delete(EDBConnectionType eDBConnectionType, Expression<Func<T, bool>> whereExpress)
        {
            return await base.Delete(eDBConnectionType, whereExpress);
        }
    }
}
