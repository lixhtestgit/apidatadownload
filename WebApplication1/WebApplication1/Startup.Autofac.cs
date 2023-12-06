using Autofac;
using System.Reflection;
using WebApplication1.DB.Base;

namespace WebApplication1
{
    /// <summary>
    /// 启动分布类
    /// </summary>
    public partial class Startup
    {
        /// <summary>
        /// autofac configure
        /// https://github.com/1100100/Dapper.Extensions
        /// </summary>
        /// <param name="containerBuilder"></param>
        public void ConfigureContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<BaseRepository>().InstancePerLifetimeScope();
            //获取当前程序集：Assembly.GetExecutingAssembly()
            //注册仓储程序集
            containerBuilder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                   .Where(t => typeof(BaseRepository).IsAssignableFrom(t) 
                            || t.Name.EndsWith("Helper")
                            || t.Name.EndsWith("BIZ"))
                   .InstancePerLifetimeScope();
        }
    }
}
