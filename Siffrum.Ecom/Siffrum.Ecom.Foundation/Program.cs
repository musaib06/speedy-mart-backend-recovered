namespace Siffrum.Ecom.Foundation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            CreateHostBuilder(args).Build().Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureAppConfiguration((hostingContext, x) =>
                {
                    //x.InitializeJsonConfigFiles(hostingContext.HostingEnvironment.EnvironmentName);
                    x.AddEnvironmentVariables();
                });
                webBuilder.UseDefaultServiceProvider(x =>
                {
                    // do not remove these lines
                    x.ValidateOnBuild = true;
                    x.ValidateScopes = true;
                });
                webBuilder.ConfigureKestrel(options =>
                {
                    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
                });
                webBuilder.UseStartup<Startup>();
            });
    }
}