using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace WebApplication1.DB.Extend
{
    /// <summary>
    /// Generating SQL from expression trees, Part 2
    /// http://ryanohs.com/2016/04/generating-sql-from-expression-trees-part-2/#more-394
    /// </summary>
    public class WhereBuilder
    {
        private System.Collections.ObjectModel.ReadOnlyCollection<ParameterExpression> expressParameterNameCollection;
        /// <summary>
        /// 构造
        /// </summary>
        public WhereBuilder()
        {
        }

        /// <summary>
        /// LINQ转SQL
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public WherePart ToSql<T>(Expression<Func<T, bool>> expression)
        {
            var i = 1;
            if (expression.Parameters.Count > 0)
            {
                this.expressParameterNameCollection = expression.Parameters;
            }
            return Recurse(ref i, expression.Body, isUnary: true);
        }

        /// <summary>
        /// LINQ转SQL
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="i">种子值</param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public WherePart ToSql<T>(ref int i, Expression<Func<T, bool>> expression)
        {
            if (expression.Parameters.Count > 0)
            {
                this.expressParameterNameCollection = expression.Parameters;
            }
            return Recurse(ref i, expression.Body, isUnary: true);
        }

        /// <summary>
        /// LINQ转SQL
        /// </summary>
        /// <param name="i">种子值</param>
        /// <param name="expression"></param>
        /// <param name="isUnary"></param>
        /// <param name="prefix"></param>
        /// <param name="postfix"></param>
        /// <returns></returns>
        private WherePart Recurse(ref int i, Expression expression, bool isUnary = false, string prefix = null, string postfix = null)
        {
            //运算符表达式
            if (expression is UnaryExpression)
            {
                var unary = (UnaryExpression)expression;
                //示例：m.birthday=DateTime.Now
                if (unary.NodeType == ExpressionType.Convert)
                {
                    var value = GetValue(expression);
                    if (value is string)
                    {
                        value = prefix + (string)value + postfix;
                    }
                    return WherePart.IsParameter(i++, value);
                }
                else
                {
                    //示例：m.Birthday>'2018-10-31'
                    return WherePart.Concat(NodeTypeToString(unary.NodeType), Recurse(ref i, unary.Operand, true));
                }
            }
            if (expression is BinaryExpression)
            {
                var body = (BinaryExpression)expression;
                return WherePart.Concat(Recurse(ref i, body.Left), NodeTypeToString(body.NodeType), Recurse(ref i, body.Right));
            }
            //常量值表达式
            //示例右侧表达式：m.ID=123;
            if (expression is ConstantExpression)
            {
                var constant = (ConstantExpression)expression;
                var value = constant.Value;
                if (value is int)
                {
                    return WherePart.IsSql(value.ToString());
                }
                if (value is string)
                {
                    value = prefix + (string)value + postfix;
                }
                if (value is bool && isUnary)
                {
                    return WherePart.Concat(WherePart.IsParameter(i++, value), "=", WherePart.IsSql("1"));
                }
                return WherePart.IsParameter(i++, value);
            }
            //成员表达式
            if (expression is MemberExpression)
            {
                var member = (MemberExpression)expression;
                var memberExpress = member.Expression;
                bool isContainsParameterExpress = false;
                this.IsContainsParameterExpress(member, ref isContainsParameterExpress);
                if (member.Member is PropertyInfo && isContainsParameterExpress)
                {
                    var property = (PropertyInfo)member.Member;
                    //var colName = _tableDef.GetColumnNameFor(property.Name);
                    var colName = property.Name;
                    if (isUnary && member.Type == typeof(bool))
                    {
                        return WherePart.Concat(Recurse(ref i, expression), "=", WherePart.IsParameter(i++, true));
                    }
                    return WherePart.IsSql(colName);
                }
                if (member.Member is FieldInfo || !isContainsParameterExpress)
                {
                    var value = GetValue(member);
                    if (value is string)
                    {
                        value = prefix + (string)value + postfix;
                    }
                    return WherePart.IsParameter(i++, value);
                }
                throw new Exception($"Expression does not refer to a property or field: {expression}");
            }
            //方法表达式
            if (expression is MethodCallExpression)
            {
                var methodCall = (MethodCallExpression)expression;
                //属性表达式中的参数表达式是否是表达式参数集合中的实例（或者表达式中包含的其他表达式中的参数表达式）
                bool isContainsParameterExpress = false;
                this.IsContainsParameterExpress(methodCall, ref isContainsParameterExpress);
                if (isContainsParameterExpress)
                {
                    // LIKE queries:
                    if (methodCall.Method == typeof(string).GetMethod("Contains", new[] { typeof(string) }))
                    {
                        return WherePart.Concat(Recurse(ref i, methodCall.Object), "LIKE", Recurse(ref i, methodCall.Arguments[0], prefix: "%", postfix: "%"));
                    }
                    if (methodCall.Method == typeof(string).GetMethod("StartsWith", new[] { typeof(string) }))
                    {
                        return WherePart.Concat(Recurse(ref i, methodCall.Object), "LIKE", Recurse(ref i, methodCall.Arguments[0], postfix: "%"));
                    }
                    if (methodCall.Method == typeof(string).GetMethod("EndsWith", new[] { typeof(string) }))
                    {
                        return WherePart.Concat(Recurse(ref i, methodCall.Object), "LIKE", Recurse(ref i, methodCall.Arguments[0], prefix: "%"));
                    }
                    // IN queries:
                    if (methodCall.Method.Name == "Contains")
                    {
                        Expression collection;
                        Expression property;
                        if (methodCall.Method.IsDefined(typeof(ExtensionAttribute)) && methodCall.Arguments.Count == 2)
                        {
                            collection = methodCall.Arguments[0];
                            property = methodCall.Arguments[1];
                        }
                        else if (!methodCall.Method.IsDefined(typeof(ExtensionAttribute)) && methodCall.Arguments.Count == 1)
                        {
                            collection = methodCall.Object;
                            property = methodCall.Arguments[0];
                        }
                        else
                        {
                            throw new Exception("Unsupported method call: " + methodCall.Method.Name);
                        }
                        var values = (IEnumerable)GetValue(collection);
                        return WherePart.Concat(Recurse(ref i, property), "IN", WherePart.IsCollection(ref i, values));
                    }
                }
                else
                {
                    var value = GetValue(expression);
                    if (value is string)
                    {
                        value = prefix + (string)value + postfix;
                    }
                    return WherePart.IsParameter(i++, value);
                }

                throw new Exception("Unsupported method call: " + methodCall.Method.Name);
            }
            //New表达式
            if (expression is NewExpression)
            {
                var member = (NewExpression)expression;
                var value = GetValue(member);
                if (value is string)
                {
                    value = prefix + (string)value + postfix;
                }
                return WherePart.IsParameter(i++, value);
            }
            throw new Exception("Unsupported expression: " + expression.GetType().Name);
        }
        /// <summary>
        /// 判断表达式内部是否含有变量M
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="result"></param>
        private void IsContainsParameterExpress(Expression expression, ref bool result)
        {
            if (this.expressParameterNameCollection != null && this.expressParameterNameCollection.Count > 0 && expression != null)
            {
                if (expression is MemberExpression)
                {
                    if (this.expressParameterNameCollection.Contains(((MemberExpression)expression).Expression))
                    {
                        result = true;
                    }
                }
                else if (expression is MethodCallExpression)
                {
                    MethodCallExpression methodCallExpression = (MethodCallExpression)expression;

                    if (methodCallExpression.Object != null)
                    {
                        if (methodCallExpression.Object is MethodCallExpression)
                        {
                            //判断示例1：m.ID.ToString().Contains("123")
                            this.IsContainsParameterExpress(methodCallExpression.Object, ref result);
                        }
                        else if (methodCallExpression.Object is MemberExpression)
                        {
                            //判断示例2：m.ID.Contains(123)
                            MemberExpression MemberExpression = (MemberExpression)methodCallExpression.Object;
                            if (MemberExpression.Expression != null && this.expressParameterNameCollection.Contains(MemberExpression.Expression))
                            {
                                result = true;
                            }
                        }
                    }
                    //判断示例3： int[] ids=new ids[]{1,2,3};  ids.Contains(m.ID)
                    if (result == false && methodCallExpression.Arguments != null && methodCallExpression.Arguments.Count > 0)
                    {
                        foreach (Expression express in methodCallExpression.Arguments)
                        {
                            if (express is MemberExpression || express is MethodCallExpression)
                            {
                                this.IsContainsParameterExpress(express, ref result);
                            }
                            else if (this.expressParameterNameCollection.Contains(express))
                            {
                                result = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取值
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        private static object GetValue(Expression member)
        {
            // source: http://stackoverflow.com/a/2616980/291955
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        /// <summary>
        /// 关系值
        /// </summary>
        /// <param name="nodeType"></param>
        /// <returns></returns>
        private static string NodeTypeToString(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.And:
                    return "&";
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.ExclusiveOr:
                    return "^";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.Modulo:
                    return "%";
                case ExpressionType.Multiply:
                    return "*";
                case ExpressionType.Negate:
                    return "-";
                case ExpressionType.Not:
                    return "NOT";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.Or:
                    return "|";
                case ExpressionType.OrElse:
                    return "OR";
                case ExpressionType.Subtract:
                    return "-";
            }
            throw new Exception($"Unsupported node type: {nodeType}");
        }
    }

    /// <summary>
    /// Where拼接器
    /// </summary>
    public class WherePart
    {
        /// <summary>
        /// 含有参数变量的SQL语句
        /// </summary>
        public string Sql { get; set; }
        /// <summary>
        /// SQL语句中的参数变量
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static WherePart IsSql(string sql)
        {
            return new WherePart()
            {
                Parameters = new Dictionary<string, object>(),
                Sql = sql
            };
        }

        /// <summary>
        /// 是否是参数
        /// </summary>
        /// <param name="count"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static WherePart IsParameter(int count, object value)
        {
            return new WherePart()
            {
                Parameters = { { $"a{count.ToString()}", value } },
                Sql = $"@a{count}"
            };
        }

        /// <summary>
        /// 是否是集合
        /// </summary>
        /// <param name="countStart"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static WherePart IsCollection(ref int countStart, IEnumerable values)
        {
            var parameters = new Dictionary<string, object>();
            var sql = new StringBuilder("(");
            foreach (var value in values)
            {
                parameters.Add($"a{countStart.ToString()}", value);
                sql.Append($"@a{countStart},");
                countStart++;
            }
            if (sql.Length == 1)
            {
                sql.Append("null,");
            }
            sql[sql.Length - 1] = ')';
            return new WherePart()
            {
                Parameters = parameters,
                Sql = sql.ToString()
            };
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="operator"></param>
        /// <param name="operand"></param>
        /// <returns></returns>
        public static WherePart Concat(string @operator, WherePart operand)
        {
            return new WherePart()
            {
                Parameters = operand.Parameters,
                Sql = $"({@operator} {operand.Sql})"
            };
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="left"></param>
        /// <param name="operator"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static WherePart Concat(WherePart left, string @operator, WherePart right)
        {
            return new WherePart()
            {
                Parameters = left.Parameters.Union(right.Parameters).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                Sql = $"({left.Sql} {@operator} {right.Sql})"
            };
        }
    }
}
