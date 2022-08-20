using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WebApplication1.DB.Extend
{
    /// <summary>
    /// 排序方式
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class Sort<TEntity>
    {
        private readonly Dictionary<string, int> sorts = new Dictionary<string, int>();
        internal Sort()
        { }

        /// <summary>
        /// 正序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Sort<TEntity> Asc<T>(Expression<Func<TEntity, T>> expression)
        {
            var name = GetPropertyName(expression);
            sorts[name] = 1;
            return this;
        }
        /// <summary>
        /// 正序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public Sort<TEntity> Asc<T>(string propertyName)
        {
            sorts[propertyName] = 1;
            return this;
        }
        /// <summary>
        /// 倒序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Sort<TEntity> Desc<T>(Expression<Func<TEntity, T>> expression)
        {
            var name = GetPropertyName(expression);
            sorts[name] = -1;
            return this;
        }
        /// <summary>
        /// 倒序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public Sort<TEntity> Desc<T>(string propertyName)
        {
            sorts[propertyName] = -1;
            return this;
        }

        /// <summary>
        /// 获取指定对象名称
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        protected string GetPropertyName<T>(Expression<Func<TEntity, T>> expr)
        {
            if (expr.Body is UnaryExpression)
            {
                return ((MemberExpression)((UnaryExpression)expr.Body).Operand).Member.Name;
            }
            else if (expr.Body is MemberExpression)
            {
                return ((MemberExpression)expr.Body).Member.Name;
            }
            else if (expr.Body is ParameterExpression)
            {
                return ((ParameterExpression)expr.Body).Type.Name;
            }
            return null;
        }
        /// <summary>
        /// tosql
        /// </summary>
        /// <returns></returns>
        internal string ToSql()
        {
            if (sorts.Count == 0)
                return string.Empty;
            string order = string.Empty;// " ORDER BY";
            foreach (var item in sorts)
            {
                order += "`" + item.Key + "` " + (item.Value == 1 ? "ASC" : "DESC") + ",";
            }
            return order.Trim(',');
        }
    }
}
