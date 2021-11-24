using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                }).ConfigureAppConfiguration((hostingContext, config) =>
                {
                    IHostEnvironment hostEnvironment = hostingContext.HostingEnvironment;
                    string environmentName = hostEnvironment.EnvironmentName;
                    config.AddJsonFile("en-Template1.json", false, true);
                    config.AddJsonFile("en-Template2.json", false, true);
                    config.AddJsonFile("en-Template3.json", false, true);
                    config.AddJsonFile("en-Template4.json", false, true);
                    config.AddJsonFile("en-Template1007.json", false, true);
                });
    }
}
